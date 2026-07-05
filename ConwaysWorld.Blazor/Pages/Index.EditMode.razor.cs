using ConwaysWorld.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ConwaysWorld.Blazor.Pages;

public partial class Index
{
        // ── Edit Mode entry / exit ────────────────────────────────────────────────────

        private async Task ToggleEditMode()
        {
                _editMode = !_editMode;
                if (_editMode)
                {
                        _editWasRunning = _running;
                        _running = false;
                        _timer.Stop();
                        _editSidebarTab = 1;
                        _editMoveMode = false;
                        _currentStrokeCells.Clear();
                }
                else
                {
                        _editMoveMode = false;
                        _currentStrokeCells.Clear();
                        _editSidebarTab = 0;
                        if (_editWasRunning)
                        {
                                _running = true;
                                _timer.Start();
                        }
                }
                await JS.InvokeVoidAsync("ConwaysInterop.setEditMode", _editMode, false);
                StateHasChanged();
        }

        private async Task ToggleMoveMode()
        {
                _editMoveMode = !_editMoveMode;
                await JS.InvokeVoidAsync("ConwaysInterop.setEditMode", _editMode, _editMoveMode);
                StateHasChanged();
        }

        private async Task SelectBrush(int brushType)
        {
                _editBrushType = brushType;
                if (_editMoveMode)
                {
                        _editMoveMode = false;
                        await JS.InvokeVoidAsync("ConwaysInterop.setEditMode", _editMode, false);
                }
                StateHasChanged();
        }

        private void OnEditNationChange(ChangeEventArgs e)
        {
                if (int.TryParse(e.Value?.ToString(), out var n))
                        _editNation = n;
        }

        // ── Paint / erase / stroke ────────────────────────────────────────────────────

        [JSInvokable]
        public void OnEditPaint(int col, int row)
        {
                if (col < 0 || row < 0 || col >= _model.Columns || row >= _model.Rows)
                        return;
                if (_currentStrokeCells.ContainsKey((col, row)))
                        return;

                var oldCell = _model.CellGrid[col, row];
                bool oldAlive = oldCell.IsAlive;
                CellType oldType = oldCell.CellType;
                int oldNat = oldCell.Nationality;

                bool newAlive;
                CellType newType;
                int newNat;

                if (_editBrushType == -1)
                {
                        _model.RemoveCell(col, row);
                        newAlive = false;
                        newType = CellType.Dead;
                        newNat = -1;
                }
                else
                {
                        var brushType = (CellType)_editBrushType;
                        int nation = IsNationCapable(brushType) ? _editNation : -1;
                        _model.PlaceCell(col, row, brushType, nation);
                        newAlive = true;
                        newType = brushType;
                        newNat = nation;
                }

                _currentStrokeCells[(col, row)] =
                                new EditSnapshot(col, row, oldAlive, oldType, oldNat, newAlive, newType, newNat);

                _ = InvokeAsync(async () =>
                {
                        CapturePrevCells();
                        await RenderFrame();
                        UpdateTypeCounts();
                        StateHasChanged();
                });
        }

        [JSInvokable]
        public void OnEditErase(int col, int row)
        {
                if (col < 0 || row < 0 || col >= _model.Columns || row >= _model.Rows)
                        return;
                if (_currentStrokeCells.ContainsKey((col, row)))
                        return;

                var oldCell = _model.CellGrid[col, row];
                if (!oldCell.IsAlive)
                        return;

                var snap = new EditSnapshot(col, row, true, oldCell.CellType, oldCell.Nationality, false, CellType.Dead, -1);
                _model.RemoveCell(col, row);
                _currentStrokeCells[(col, row)] = snap;

                _ = InvokeAsync(async () =>
                {
                        CapturePrevCells();
                        await RenderFrame();
                        UpdateTypeCounts();
                        StateHasChanged();
                });
        }

        [JSInvokable]
        public void OnEditStrokeEnd()
        {
                if (_currentStrokeCells.Count > 0)
                {
                        _undoStack.AddLast(_currentStrokeCells.Values.ToList());
                        while (_undoStack.Count > MaxUndoHistory)
                                _undoStack.RemoveFirst();
                        _redoStack.Clear();
                        _currentStrokeCells.Clear();
                }
                InvokeAsync(StateHasChanged);
        }

        [JSInvokable]
        public void OnEditSelectCell(int col, int row)
        {
                // Acknowledgement callback — JS maintains the selection state.
                _ = InvokeAsync(StateHasChanged);
        }

        [JSInvokable]
        public void OnEditMoveDrop(int fromCol, int fromRow, int toCol, int toRow)
        {
                if (fromCol < 0 || fromCol >= _model.Columns || fromRow < 0 || fromRow >= _model.Rows)
                        return;
                if (toCol < 0 || toCol >= _model.Columns || toRow < 0 || toRow >= _model.Rows)
                        return;
                if (fromCol == toCol && fromRow == toRow)
                        return;

                var fromCell = _model.CellGrid[fromCol, fromRow];
                if (!fromCell.IsAlive)
                        return;
                var toCell = _model.CellGrid[toCol, toRow];

                var snapshots = new List<EditSnapshot>
                                {
                                                new(fromCol, fromRow,
                                                        fromCell.IsAlive, fromCell.CellType, fromCell.Nationality,
                                                        false, CellType.Dead, -1),
                                                new(toCol, toRow,
                                                        toCell.IsAlive, toCell.CellType, toCell.Nationality,
                                                        fromCell.IsAlive, fromCell.CellType, fromCell.Nationality),
                                };

                _model.MoveCell(fromCol, fromRow, toCol, toRow);
                _undoStack.AddLast(snapshots);
                while (_undoStack.Count > MaxUndoHistory)
                        _undoStack.RemoveFirst();
                _redoStack.Clear();

                _ = InvokeAsync(async () =>
                {
                        CapturePrevCells();
                        await RenderFrame();
                        UpdateTypeCounts();
                        StateHasChanged();
                });
        }

        [JSInvokable]
        public async Task OnKeyEdit()
        {
                await ToggleEditMode();
        }

        // ── Undo / Redo / Clear ───────────────────────────────────────────────────────

        private async Task UndoEdit()
        {
                if (_undoStack.Count == 0)
                        return;
                var entry = _undoStack.Last!.Value;
                _undoStack.RemoveLast();
                foreach (var snap in entry)
                        _model.RestoreCell(snap.Col, snap.Row, snap.OldAlive, snap.OldType, snap.OldNat);
                _redoStack.AddLast(entry);
                while (_redoStack.Count > MaxUndoHistory)
                        _redoStack.RemoveFirst();
                CapturePrevCells();
                await RenderFrame();
                UpdateTypeCounts();
                StateHasChanged();
        }

        private async Task RedoEdit()
        {
                if (_redoStack.Count == 0)
                        return;
                var entry = _redoStack.Last!.Value;
                _redoStack.RemoveLast();
                foreach (var snap in entry)
                        _model.RestoreCell(snap.Col, snap.Row, snap.NewAlive, snap.NewType, snap.NewNat);
                _undoStack.AddLast(entry);
                while (_undoStack.Count > MaxUndoHistory)
                        _undoStack.RemoveFirst();
                CapturePrevCells();
                await RenderFrame();
                UpdateTypeCounts();
                StateHasChanged();
        }

        private async Task ClearAllEdit()
        {
                var cleared = _model.ClearAllCells();
                if (cleared.Count > 0)
                {
                        var snapshots = cleared.Select(s =>
                                        new EditSnapshot(s.Col, s.Row, true, s.Type, s.Nat, false, CellType.Dead, -1)).ToList();
                        _undoStack.AddLast(snapshots);
                        while (_undoStack.Count > MaxUndoHistory)
                                _undoStack.RemoveFirst();
                        _redoStack.Clear();
                }
                CapturePrevCells();
                await RenderFrame();
                UpdateTypeCounts();
                StateHasChanged();
        }
}
