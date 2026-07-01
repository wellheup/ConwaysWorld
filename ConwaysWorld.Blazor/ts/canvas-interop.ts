interface CellData {
    col: number;
    row: number;
    type: number;
    nat: number;
    alive: boolean;
}

interface MoveData {
    fromCol: number;
    fromRow: number;
    toCol: number;
    toRow: number;
    type: number;
    nat: number;
}

interface SpecialCellData {
    col: number;
    row: number;
    type: number;
    nat: number;
}

interface GridCell {
    col: number;
    row: number;
}

interface FamineData {
    active: boolean;
    quadrant: number; // 0=NW, 1=NE, 2=SW, 3=SE
}

interface FloodData {
    active: boolean;
}

interface DotNetRef {
    invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
}

(window as any).ConwaysInterop = (() => {
    let canvas: HTMLCanvasElement | null = null;
    let ctx: CanvasRenderingContext2D | null = null;
    let cellSize = 14;
    let cols = 0,
        rows = 0;
    let scale = 1,
        tx = 0,
        ty = 0;
    let userHasTransformed = false;
    let isPanning = false;
    let panStart = { x: 0, y: 0 };
    let dotnetRef: DotNetRef | null = null;
    let hoveredCell: GridCell | null = null;
    let selectedCell: GridCell | null = null;
    let isAnimating = false;

    // ── Edit mode state ───────────────────────────────────────────────────────
    let editMode = false;
    let editMoveMode = false;
    let editButtonDown = false;
    let editEraseButtonDown = false;
    let editMoveSelected: GridCell | null = null; // persistent move-mode selection
    let editMoveWasSelectedBeforeMouseDown = false; // deselect tracking

    let cachedCells: CellData[] = [];
    let cachedNationColors: string[] = [];
    let cachedFamine: FamineData = { active: false, quadrant: 0 };
    let cachedFlood: FloodData = { active: false };
    let rafPending = false;

    const SETTINGS_KEY = 'cw_settings';

    const SPRITE_NAMES: string[] = [
        'Dead',
        'Basic',
        'Immortal',
        'Diseased',
        'Plague',
        'Traveler',
        'Explorer',
        'Doctor',
        'Warrior',
        'Hunter',
        'Bomber',
        'Diplomat',
        'King',
        'Rebel',
        'Revolutionary',
        'Voyager',
        'Wayfinder',
        'Islander',
        'Barbarian',
        'Spy',
        'Soldier',
        'Conquistador',
        'Savior',
        'Follower',
        'Zealot',
        'Zombie',
        'Necromancer',
        'Irradiated',
        'PlagueRat',
        'Mutant',
    ];

    const TYPE_COLORS: Record<number, string> = {
        0: '#111',
        1: '#e8e8e8',
        2: '#e0c060',
        3: '#7a2020',
        4: '#c01010',
        5: '#4090d0',
        6: '#20c0e0',
        7: '#e050a0',
        8: '#d08020',
        9: '#c04040',
        10: '#e0a000',
        11: '#a060e0',
        12: '#f0d000',
        13: '#ff5533',
        14: '#9b1a4a',
        15: '#00d4aa',
        16: '#1e90ff',
        17: '#c8a040',
        18: '#bb2200',
        19: '#3a3a5a',
        20: '#5a9e20',
        21: '#c87800',
        22: '#ffffff',
        23: '#b0e0ff',
        24: '#ff4400',
        25: '#111111',
        26: '#111111',
        27: '#55ff00',
        28: '#8b2020',
        29: '#cc44ff',
    };

    const sprites: { [key: number]: HTMLImageElement } = {};

    function loadSprites(): Promise<void[]> {
        const base = (document.querySelector('base') as HTMLBaseElement)?.href ?? '/';
        const promises = SPRITE_NAMES.map(
            (name, i) =>
                new Promise<void>(resolve => {
                    const img = new Image();
                    img.onload = () => {
                        sprites[i] = img;
                        resolve();
                    };
                    img.onerror = () => resolve();
                    img.src = `${base}Assets/Sprites/Cell_${name}.jpg?v=3`;
                }),
        );
        return Promise.all(promises);
    }

    async function init(canvasId: string, c: number, r: number, cs: number, ref: DotNetRef): Promise<void> {
        canvas = document.getElementById(canvasId) as HTMLCanvasElement;
        ctx = canvas.getContext('2d');
        cols = c;
        rows = r;
        cellSize = cs;
        dotnetRef = ref;
        await loadSprites();
        fitCanvas();
        bindEvents();
        bindSpriteZoom();
    }

    function bindSpriteZoom(): void {
        document.addEventListener('mouseover', (e: MouseEvent) => {
            const wrap = (e.target as Element).closest?.('.cw-sprite-wrap') as HTMLElement | null;
            if (!wrap) return;
            const popup = wrap.querySelector('.cw-sprite-zoom-popup') as HTMLElement | null;
            if (!popup) return;
            const rect = wrap.getBoundingClientRect();
            const popupW = 200;
            const popupH = 200;
            const gap = 10;
            let left = rect.left - popupW - gap;
            if (left < 4) left = rect.right + gap;
            let top = rect.top + rect.height / 2 - popupH / 2;
            top = Math.max(4, Math.min(top, window.innerHeight - popupH - 4));
            popup.style.left = `${left}px`;
            popup.style.top = `${top}px`;
            popup.style.display = 'block';
        });
        document.addEventListener('mouseout', (e: MouseEvent) => {
            const wrap = (e.target as Element).closest?.('.cw-sprite-wrap') as HTMLElement | null;
            if (!wrap) return;
            const related = e.relatedTarget as Element | null;
            if (related && wrap.contains(related)) return;
            const popup = wrap.querySelector('.cw-sprite-zoom-popup') as HTMLElement | null;
            if (popup) popup.style.display = 'none';
        });
    }

    function fitCanvas(): void {
        if (!canvas) return;
        const wrap = canvas.parentElement!;
        canvas.width = wrap.clientWidth;
        canvas.height = wrap.clientHeight;
        if (!userHasTransformed) fitToWindow();
        else centerGrid();
    }

    function fitToWindow(): void {
        if (!canvas || !cols || !rows) return;
        const fitScale = Math.min(canvas.width / (cols * cellSize), canvas.height / (rows * cellSize)) * 0.97;
        scale = Math.max(0.1, fitScale);
        centerGrid();
    }

    function centerGrid(): void {
        if (!canvas) return;
        tx = (canvas.width - cols * cellSize * scale) / 2;
        ty = (canvas.height - rows * cellSize * scale) / 2;
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
        canvas.addEventListener('contextmenu', e => e.preventDefault());
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

    function scheduleRedraw(): void {
        if (isAnimating) return;
        if (rafPending) return;
        rafPending = true;
        requestAnimationFrame(() => {
            rafPending = false;
            drawFrame();
        });
    }

    function toggleFullscreen(): void {
        if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen().catch(() => {});
        } else {
            document.exitFullscreen().catch(() => {});
        }
    }

    function onKeyDown(e: KeyboardEvent): void {
        if (!dotnetRef) return;
        if (e.code === 'Space') {
            e.preventDefault();
            dotnetRef.invokeMethodAsync('OnKeyTogglePlay');
        } else if (e.code === 'KeyR') {
            dotnetRef.invokeMethodAsync('OnKeyRestart');
        } else if (e.code === 'KeyF') {
            toggleFullscreen();
        } else if (e.code === 'Escape') {
            dotnetRef.invokeMethodAsync('OnKeyEscape');
        } else if (e.code === 'KeyE') {
            dotnetRef.invokeMethodAsync('OnKeyEdit');
        }
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
                    // Dragged to a different cell — complete the move.
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
                // Same cell on mouseup: keep selection (click-to-select; onClick may deselect).
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
                // Second click on the already-selected cell: deselect.
                editMoveSelected = null;
                scheduleRedraw();
            }
            // First click: selection already applied in onMouseDown.
            // Drag to a different cell: already handled in onMouseUp.
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

    function drawCell(
        px: number,
        py: number,
        cs: number,
        type: number,
        nat: number,
        nationColors: string[],
        col: number,
        row: number,
    ): void {
        if (!ctx) return;
        const w = cs - 1;
        const nationColor = nat >= 0 && nat < nationColors.length && nationColors[nat] ? nationColors[nat] : '#222';

        ctx.fillStyle = nationColor;
        ctx.fillRect(px, py, w, w);

        if (cs * scale > 20) {
            if (sprites[type]) {
                ctx.drawImage(sprites[type], px + 1, py + 1, w - 2, w - 2);
            } else {
                const inner = Math.max(2, Math.floor(cs * 0.45));
                const off = Math.floor((cs - inner) / 2);
                ctx.fillStyle = TYPE_COLORS[type] ?? '#fff';
                ctx.fillRect(px + off, py + off, inner, inner);
            }
        }

        if (selectedCell && col >= 0 && selectedCell.col === col && selectedCell.row === row) {
            const lw = Math.max(2, Math.round(3 / scale));
            const half = lw / 2;
            ctx.strokeStyle = '#ffff00';
            ctx.lineWidth = lw;
            ctx.strokeRect(px + half, py + half, w - lw, w - lw);
            ctx.strokeStyle = '#000';
            ctx.lineWidth = Math.max(1, Math.round(1 / scale));
            ctx.strokeRect(px + half + lw, py + half + lw, w - lw * 3, w - lw * 3);
        }
    }

    function drawCellScaled(
        col: number,
        row: number,
        cs: number,
        type: number,
        nat: number,
        nationColors: string[],
        sizeFactor: number,
    ): void {
        if (!ctx || sizeFactor <= 0) return;
        const fullW = cs - 1;
        const w = fullW * sizeFactor;
        if (w < 1) return;
        const offset = (fullW - w) / 2;
        const px = col * cs + offset;
        const py = row * cs + offset;
        const nationColor = nat >= 0 && nat < nationColors.length && nationColors[nat] ? nationColors[nat] : '#222';
        ctx.fillStyle = nationColor;
        ctx.fillRect(px, py, w, w);
        if (w * scale > 20) {
            if (sprites[type]) {
                const inner = Math.max(1, w - 2);
                ctx.drawImage(sprites[type], px + 1, py + 1, inner, inner);
            } else {
                const inner = Math.max(1, cs * 0.45 * sizeFactor);
                const innerOff = (w - inner) / 2;
                ctx.fillStyle = TYPE_COLORS[type] ?? '#fff';
                ctx.fillRect(px + innerOff, py + innerOff, inner, inner);
            }
        }
    }

    function drawCellScaledRotated(
        col: number,
        row: number,
        cs: number,
        type: number,
        nat: number,
        nationColors: string[],
        sizeFactor: number,
        angleDeg: number,
    ): void {
        if (!ctx || sizeFactor <= 0) return;
        if (!angleDeg) {
            drawCellScaled(col, row, cs, type, nat, nationColors, sizeFactor);
            return;
        }
        const cx = (col + 0.5) * cs;
        const cy = (row + 0.5) * cs;
        ctx.save();
        ctx.translate(cx, cy);
        ctx.rotate((angleDeg * Math.PI) / 180);
        ctx.translate(-cx, -cy);
        drawCellScaled(col, row, cs, type, nat, nationColors, sizeFactor);
        ctx.restore();
    }

    function drawFloodOverlay(): void {
        if (!ctx || !cachedFlood.active) return;
        const w = cols * cellSize;
        const h = rows * cellSize;
        ctx.fillStyle = 'rgba(0, 60, 160, 0.13)';
        ctx.fillRect(0, 0, w, h);
        const fontSize = Math.max(6, Math.min(16, cellSize));
        ctx.save();
        ctx.font = `bold ${fontSize}px sans-serif`;
        ctx.fillStyle = 'rgba(0, 80, 200, 0.70)';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText('\uD83C\uDF0A FLOOD', w / 2, h / 2);
        ctx.restore();
    }

    function drawFamineOverlay(): void {
        if (!ctx || !cachedFamine.active) return;
        const cs = cellSize;
        const halfCols = Math.floor(cols / 2);
        const halfRows = Math.floor(rows / 2);
        const q = cachedFamine.quadrant;
        const startCol = q === 1 || q === 3 ? halfCols : 0;
        const endCol = q === 1 || q === 3 ? cols : halfCols;
        const startRow = q === 2 || q === 3 ? halfRows : 0;
        const endRow = q === 2 || q === 3 ? rows : halfRows;
        const x = startCol * cs;
        const y = startRow * cs;
        const w = (endCol - startCol) * cs;
        const h = (endRow - startRow) * cs;
        ctx.fillStyle = 'rgba(160, 60, 0, 0.14)';
        ctx.fillRect(x, y, w, h);
        const fontSize = Math.max(6, Math.min(16, cs));
        ctx.save();
        ctx.font = `bold ${fontSize}px sans-serif`;
        ctx.fillStyle = 'rgba(200, 80, 0, 0.72)';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText('⚡ FAMINE', x + w / 2, y + h / 2);
        ctx.restore();
    }

    function drawFrame(): void {
        if (!ctx || !canvas) return;

        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.save();
        ctx.translate(tx, ty);
        ctx.scale(scale, scale);

        const cs = cellSize;
        const cells = cachedCells;
        const nationColors = cachedNationColors;

        for (let i = 0; i < cells.length; i++) {
            const c = cells[i];
            if (!c.alive) continue;
            drawCell(c.col * cs, c.row * cs, cs, c.type, c.nat, nationColors, c.col, c.row);
        }

        drawFamineOverlay();
        drawFloodOverlay();

        ctx.strokeStyle = '#999999';
        ctx.lineWidth = 2 / scale;
        ctx.strokeRect(0, 0, cols * cs, rows * cs);

        if (editMode && hoveredCell) {
            ctx.strokeStyle = '#00e5ff';
            ctx.lineWidth = 2 / scale;
            ctx.strokeRect(hoveredCell.col * cs, hoveredCell.row * cs, cs - 1, cs - 1);
        }
        if (editMode && editMoveMode && editMoveSelected) {
            const lw = Math.max(2, Math.round(3 / scale));
            const half = lw / 2;
            ctx.strokeStyle = '#ffff00';
            ctx.lineWidth = lw;
            ctx.strokeRect(
                editMoveSelected.col * cs + half,
                editMoveSelected.row * cs + half,
                cs - 1 - lw,
                cs - 1 - lw,
            );
        }

        ctx.restore();
    }

    function easeInOut(t: number): number {
        return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
    }

    function lerp(a: number, b: number, t: number): number {
        return a + (b - a) * t;
    }

    function drawFrameAnimated(
        t: number,
        excludeSet: Set<string>,
        moves: MoveData[],
        births: SpecialCellData[],
        deaths: SpecialCellData[],
        epicDeaths: SpecialCellData[],
        coronations: SpecialCellData[],
        nationColors: string[],
    ): void {
        if (!ctx || !canvas) return;

        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.save();
        ctx.translate(tx, ty);
        ctx.scale(scale, scale);

        const cs = cellSize;

        for (let i = 0; i < cachedCells.length; i++) {
            const c = cachedCells[i];
            if (!c.alive) continue;
            if (excludeSet.has(c.col + ',' + c.row)) continue;
            drawCell(c.col * cs, c.row * cs, cs, c.type, c.nat, nationColors, c.col, c.row);
        }

        for (let i = 0; i < moves.length; i++) {
            const m = moves[i];
            const isWrapped = Math.abs(m.fromCol - m.toCol) > 1 || Math.abs(m.fromRow - m.toRow) > 1;
            let px: number, py: number;
            if (isWrapped) {
                px = m.toCol * cs;
                py = m.toRow * cs;
            } else {
                px = lerp(m.fromCol * cs, m.toCol * cs, t);
                py = lerp(m.fromRow * cs, m.toRow * cs, t);
            }
            drawCell(px, py, cs, m.type, m.nat, nationColors, -1, -1);
        }

        for (let i = 0; i < deaths.length; i++) {
            const d = deaths[i];
            drawCellScaled(d.col, d.row, cs, d.type, d.nat, nationColors, 1 - t);
        }

        for (let i = 0; i < births.length; i++) {
            const b = births[i];
            drawCellScaled(b.col, b.row, cs, b.type, b.nat, nationColors, t);
        }

        for (let i = 0; i < epicDeaths.length; i++) {
            const d = epicDeaths[i];
            let sf: number;
            if (t < 0.3) {
                sf = 1 + (t / 0.3) * 0.55;
            } else {
                sf = 1.55 * (1 - (t - 0.3) / 0.7);
            }
            drawCellScaledRotated(d.col, d.row, cs, d.type, d.nat, nationColors, sf, t * 300);
        }

        for (let i = 0; i < coronations.length; i++) {
            const k = coronations[i];
            drawCellScaledRotated(k.col, k.row, cs, k.type, k.nat, nationColors, 1 + 0.55 * Math.sin(Math.PI * t), 0);
        }

        drawFamineOverlay();
        drawFloodOverlay();

        ctx.strokeStyle = '#999999';
        ctx.lineWidth = 2 / scale;
        ctx.strokeRect(0, 0, cols * cs, rows * cs);

        ctx.restore();
    }

    function renderFrame(
        cells: CellData[],
        nationColors: string[],
        liveNationIndices: number[],
        newCols: number,
        newRows: number,
        moves: MoveData[],
        births: SpecialCellData[],
        deaths: SpecialCellData[],
        epicDeaths: SpecialCellData[],
        coronations: SpecialCellData[],
        animationEnabled: boolean,
        stepIntervalMs: number,
        famine: FamineData,
        flood: FloodData,
    ): Promise<void> {
        if (!ctx) return Promise.resolve();

        // Build a Set of currently-live nation indices so stale nationality tags
        // on cells from dissolved nations render as nationless (#222 background).
        const liveNatSet = new Set<number>(liveNationIndices);
        const effectiveNationColors = nationColors.map((c, i) => (liveNatSet.has(i) ? c : ''));

        const gridChanged = newCols !== cols || newRows !== rows;
        cols = newCols;
        rows = newRows;
        cachedCells = cells;
        cachedNationColors = effectiveNationColors;
        cachedFamine = famine ?? { active: false, quadrant: 0 };
        cachedFlood = flood ?? { active: false };

        let doZoom = false;
        let fromScale = 0,
            fromTx = 0,
            fromTy = 0;
        let toScale = 0,
            toTx = 0,
            toTy = 0;
        if (gridChanged && !userHasTransformed) {
            fromScale = scale;
            fromTx = tx;
            fromTy = ty;
            fitToWindow();
            toScale = scale;
            toTx = tx;
            toTy = ty;
            scale = fromScale;
            tx = fromTx;
            ty = fromTy;
            doZoom = true;
        } else if (gridChanged) {
            drawFrame();
            return Promise.resolve();
        }

        const hasMoves = moves.length > 0;
        const hasBirths = births.length > 0;
        const hasDeaths = deaths.length > 0;
        const hasEpicDeaths = epicDeaths.length > 0;
        const hasCoronations = coronations.length > 0;
        const doCellAnim = animationEnabled && (hasMoves || hasBirths || hasDeaths || hasEpicDeaths || hasCoronations);

        if (!doZoom && !doCellAnim) {
            drawFrame();
            return Promise.resolve();
        }

        const zoomDuration = 450;
        const cellDuration = stepIntervalMs * 0.65;
        const totalDuration = Math.max(doZoom ? zoomDuration : 0, doCellAnim ? cellDuration : 0);

        const excludeSet = new Set<string>();
        if (doCellAnim) {
            if (hasMoves) {
                for (let i = 0; i < moves.length; i++) {
                    excludeSet.add(moves[i].fromCol + ',' + moves[i].fromRow);
                    excludeSet.add(moves[i].toCol + ',' + moves[i].toRow);
                }
            }
            if (hasBirths) for (let i = 0; i < births.length; i++) excludeSet.add(births[i].col + ',' + births[i].row);
            if (hasDeaths) for (let i = 0; i < deaths.length; i++) excludeSet.add(deaths[i].col + ',' + deaths[i].row);
            if (hasEpicDeaths)
                for (let i = 0; i < epicDeaths.length; i++) excludeSet.add(epicDeaths[i].col + ',' + epicDeaths[i].row);
            if (hasCoronations)
                for (let i = 0; i < coronations.length; i++)
                    excludeSet.add(coronations[i].col + ',' + coronations[i].row);
        }

        return new Promise<void>(resolve => {
            const startTime = performance.now();
            isAnimating = true;

            function frame(now: number): void {
                const elapsed = now - startTime;

                if (doZoom) {
                    const zT = easeInOut(Math.min(1.0, elapsed / zoomDuration));
                    scale = lerp(fromScale, toScale, zT);
                    tx = lerp(fromTx, toTx, zT);
                    ty = lerp(fromTy, toTy, zT);
                }

                if (doCellAnim && elapsed < cellDuration) {
                    const cT = easeInOut(Math.min(1.0, elapsed / cellDuration));
                    drawFrameAnimated(cT, excludeSet, moves, births, deaths, epicDeaths, coronations, nationColors);
                } else {
                    drawFrame();
                }

                if (elapsed < totalDuration) {
                    requestAnimationFrame(frame);
                } else {
                    if (doZoom) {
                        scale = toScale;
                        tx = toTx;
                        ty = toTy;
                    }
                    isAnimating = false;
                    drawFrame();
                    resolve();
                }
            }

            requestAnimationFrame(frame);
        });
    }

    function getCanvasSize(): { width: number; height: number } {
        if (!canvas) return { width: 800, height: 600 };
        return { width: canvas.width, height: canvas.height };
    }

    function updateGridSize(c: number, r: number): void {
        cols = c;
        rows = r;
    }

    function setEditMode(enabled: boolean, moveMode: boolean): void {
        editMode = enabled;
        editMoveMode = moveMode;
        editButtonDown = false;
        editEraseButtonDown = false;
        editMoveSelected = null;
        if (!enabled) hoveredCell = null;
        scheduleRedraw();
    }

    function saveSettings(json: string): void {
        try {
            localStorage.setItem(SETTINGS_KEY, json);
        } catch (_e) {}
    }

    function loadSettings(): string | null {
        try {
            return localStorage.getItem(SETTINGS_KEY) || null;
        } catch (_e) {
            return null;
        }
    }

    function clearSettings(): void {
        try {
            localStorage.removeItem(SETTINGS_KEY);
        } catch (_e) {}
    }

    function getCanvasWrapSize(): { width: number; height: number } {
        const el = document.getElementById('canvas-wrap');
        if (!el) return { width: 800, height: 600 };
        return { width: el.clientWidth, height: el.clientHeight };
    }

    return {
        init,
        renderFrame,
        getCanvasSize,
        updateGridSize,
        saveSettings,
        loadSettings,
        clearSettings,
        toggleFullscreen,
        setEditMode,
        getCanvasWrapSize,
    };
})();
