// ── Canvas event handlers ─────────────────────────────────────────────────────
// Mouse, keyboard, and resize handlers — reference globals in canvas-state.ts.

function screenToCell(e: MouseEvent): GridCell | null {
    if (!canvas) return null;
    const rect = canvas.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;
    const col = Math.floor((mx - tx) / (cellSize * scale));
    const row = Math.floor((my - ty) / (cellSize * scale));
    if (col < 0 || col >= cols || row < 0 || row >= rows) return null;
    return { col, row };
}

function onWheel(e: WheelEvent): void {
    e.preventDefault();
    if (!canvas) return;
    const rect = canvas.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;
    const factor = e.deltaY < 0 ? 1.1 : 0.9;
    const newScale = Math.max(0.2, Math.min(10, scale * factor));

    if (e.deltaY > 0 && userHasTransformed) {
        const fitScale = Math.min(canvas.width / (cols * cellSize), canvas.height / (rows * cellSize)) * 0.97;
        if (newScale <= Math.max(fitScale, 0.2)) {
            userHasTransformed = false;
            fitToWindow();
            scheduleRedraw();
            return;
        }
    }

    userHasTransformed = true;
    scale = newScale;
    tx = mx - (mx - tx) * factor;
    ty = my - (my - ty) * factor;
    scheduleRedraw();
}

function onMouseDown(e: MouseEvent): void {
    if (e.button === 2) {
        if (editMode) {
            e.preventDefault();
            editEraseButtonDown = true;
            const cell = screenToCell(e);
            if (cell && dotnetRef) dotnetRef.invokeMethodAsync('OnEditErase', cell.col, cell.row);
        } else {
            userHasTransformed = true;
            isPanning = true;
            panStart = { x: e.clientX - tx, y: e.clientY - ty };
            canvas!.style.cursor = 'grabbing';
        }
    } else if (e.button === 0 && editMode) {
        if (editMoveMode) {
            const cell = hoveredCell;
            if (cell) {
                editMoveWasSelectedBeforeMouseDown = !!(
                    editMoveSelected &&
                    editMoveSelected.col === cell.col &&
                    editMoveSelected.row === cell.row
                );
                editMoveSelected = cell;
                scheduleRedraw();
            }
        } else {
            editButtonDown = true;
            const cell = screenToCell(e);
            if (cell && dotnetRef) dotnetRef.invokeMethodAsync('OnEditPaint', cell.col, cell.row);
        }
    }
}

function onMouseMove(e: MouseEvent): void {
    if (isPanning) {
        tx = e.clientX - panStart.x;
        ty = e.clientY - panStart.y;
        scheduleRedraw();
    }
    if (!canvas) return;
    const rect = canvas.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;
    const cell = screenToCell(e);
    if (cell && (hoveredCell === null || cell.col !== hoveredCell.col || cell.row !== hoveredCell.row)) {
        hoveredCell = cell;
        if (dotnetRef) dotnetRef.invokeMethodAsync('OnHover', cell.col, cell.row, mx, my);
        if (editMode) {
            scheduleRedraw();
            if (editButtonDown && !editMoveMode) {
                if (dotnetRef) dotnetRef.invokeMethodAsync('OnEditPaint', cell.col, cell.row);
            }
            if (editEraseButtonDown) {
                if (dotnetRef) dotnetRef.invokeMethodAsync('OnEditErase', cell.col, cell.row);
            }
        }
    } else if (!cell && hoveredCell !== null) {
        hoveredCell = null;
        if (dotnetRef) dotnetRef.invokeMethodAsync('OnHover', -1, -1, 0, 0);
        if (editMode) scheduleRedraw();
    }
}

function onMouseLeave(): void {
    hoveredCell = null;
    if (dotnetRef) dotnetRef.invokeMethodAsync('OnHover', -1, -1, 0, 0);
}

function onMouseUp(e: MouseEvent): void {
    if (e.button === 2) {
        if (editMode) {
            editEraseButtonDown = false;
            if (dotnetRef) dotnetRef.invokeMethodAsync('OnEditStrokeEnd');
        } else {
            isPanning = false;
            canvas!.style.cursor = 'crosshair';
        }
    } else if (e.button === 0 && editMode) {
        if (editMoveMode) {
            const dropCell = screenToCell(e);
            if (
                editMoveSelected &&
                dropCell &&
                (editMoveSelected.col !== dropCell.col || editMoveSelected.row !== dropCell.row)
            ) {
                if (dotnetRef)
                    dotnetRef.invokeMethodAsync(
                        'OnEditMoveDrop',
                        editMoveSelected.col,
                        editMoveSelected.row,
                        dropCell.col,
                        dropCell.row,
                    );
                editMoveSelected = null;
                scheduleRedraw();
            }
        } else {
            editButtonDown = false;
            if (dotnetRef) dotnetRef.invokeMethodAsync('OnEditStrokeEnd');
        }
    }
}

function onDblClick(): void {
    userHasTransformed = false;
    fitToWindow();
    scheduleRedraw();
}

function onClick(e: MouseEvent): void {
    if (editMode && editMoveMode) {
        const cell = screenToCell(e);
        if (
            cell &&
            editMoveSelected &&
            editMoveSelected.col === cell.col &&
            editMoveSelected.row === cell.row &&
            editMoveWasSelectedBeforeMouseDown
        ) {
            editMoveSelected = null;
            scheduleRedraw();
        }
        return;
    }
    if (editMode) return;
    const cell = screenToCell(e);
    if (cell) {
        selectedCell = cell;
        scheduleRedraw();
        if (dotnetRef) dotnetRef.invokeMethodAsync('OnCellClick', cell.col, cell.row);
    }
}

function onContextMenu(e: MouseEvent): void {
    if (editMode) e.preventDefault();
}

function bindEvents(): void {
    if (!canvas) return;
    canvas.addEventListener('wheel', onWheel, { passive: false });
    canvas.addEventListener('mousedown', onMouseDown);
    canvas.addEventListener('mousemove', onMouseMove);
    canvas.addEventListener('mouseup', onMouseUp);
    canvas.addEventListener('dblclick', onDblClick);
    canvas.addEventListener('click', onClick);
    canvas.addEventListener('mouseleave', onMouseLeave);
    canvas.addEventListener('contextmenu', onContextMenu);
    window.addEventListener('resize', () => {
        fitCanvas();
        scheduleRedraw();
    });
    window.addEventListener('keydown', onKeyDown);
    document.addEventListener('fullscreenchange', () => {
        if (dotnetRef) {
            dotnetRef.invokeMethodAsync('OnFullscreenChange', !!document.fullscreenElement);
        }
    });
}
