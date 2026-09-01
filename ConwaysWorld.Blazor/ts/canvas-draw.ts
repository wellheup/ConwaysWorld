// ── Drawing functions ─────────────────────────────────────────────────────────
// Pure rendering helpers — all reference the globals in canvas-state.ts.

function easeInOut(t: number): number {
    return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
}

function lerp(a: number, b: number, t: number): number {
    return a + (b - a) * t;
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

const MIN_SCALE = 0.001;

function getVisibleCanvasArea(): { left: number; top: number; width: number; height: number } {
    if (!canvas) return { left: 0, top: 0, width: 1, height: 1 };

    const canvasRect = canvas.getBoundingClientRect();
    const toolbar = document.querySelector('.cw-toolbar') as HTMLElement | null;
    const sidebar = document.querySelector('.cw-sidebar') as HTMLElement | null;
    const toolbarRect = toolbar?.getBoundingClientRect();
    const sidebarRect = sidebar?.getBoundingClientRect();

    // The panes are fixed overlays, so convert their rendered rectangles into
    // canvas-local bounds rather than relying on the canvas being at (0, 0).
    let top = 0;
    if (
        toolbarRect &&
        toolbarRect.bottom > canvasRect.top &&
        toolbarRect.top < canvasRect.bottom &&
        toolbarRect.right > canvasRect.left &&
        toolbarRect.left < canvasRect.right
    ) {
        top = Math.max(0, Math.min(canvasRect.height, toolbarRect.bottom - canvasRect.top));
    }

    let right = canvasRect.width;
    if (
        sidebarRect &&
        sidebarRect.left < canvasRect.right &&
        sidebarRect.right > canvasRect.left &&
        sidebarRect.top < canvasRect.bottom &&
        sidebarRect.bottom > canvasRect.top
    ) {
        right = Math.max(0, Math.min(canvasRect.width, sidebarRect.left - canvasRect.left));
    }

    return {
        left: 0,
        top,
        width: Math.max(1, right),
        height: Math.max(1, canvasRect.height - top),
    };
}

function getFitScale(): number {
    if (!cols || !rows) return MIN_SCALE;
    const view = getVisibleCanvasArea();
    return Math.min(view.width / (cols * cellSize), view.height / (rows * cellSize)) * 0.97;
}

function fitToWindow(): void {
    if (!canvas || !cols || !rows) return;
    scale = Math.max(MIN_SCALE, getFitScale());
    centerGrid();
}

function centerGrid(): void {
    if (!canvas) return;
    const view = getVisibleCanvasArea();
    tx = view.left + (view.width - cols * cellSize * scale) / 2;
    ty = view.top + (view.height - rows * cellSize * scale) / 2;
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
    if (w * scale > 20) {
        if (sprites[type]) {
            const inner = Math.max(1, w - 2);
            ctx.drawImage(sprites[type], px + 1, py + 1, inner, inner);
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
        ctx.strokeRect(editMoveSelected.col * cs + half, editMoveSelected.row * cs + half, cs - 1 - lw, cs - 1 - lw);
    }

    ctx.restore();
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
