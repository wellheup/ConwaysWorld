window.ConwaysInterop = (() => {
    let canvas, ctx;
    let cellSize = 14;
    let cols = 0, rows = 0;
    let scale = 1, tx = 0, ty = 0;
    let userHasTransformed = false;
    let isPanning = false, panStart = { x: 0, y: 0 };
    let dotnetRef = null;
    let hoveredCell = null;
    let selectedCell = null;
    let isAnimating = false;

    let cachedCells = [];
    let cachedNationColors = [];
    let rafPending = false;

    const SETTINGS_KEY = 'cw_settings';

    const SPRITE_NAMES = [
        'Dead','Basic','Immortal','Diseased','Plague',
        'Traveler','Explorer','Doctor','Warrior','Hunter',
        'Bomber','Diplomat','King'
    ];

    const TYPE_COLORS = {
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
    };

    const sprites = {};

    function loadSprites() {
        const promises = SPRITE_NAMES.map((name, i) => new Promise(resolve => {
            const img = new Image();
            img.onload = () => { sprites[i] = img; resolve(); };
            img.onerror = () => resolve();
            img.src = `Assets/Sprites/Cell_${name}.jpg`;
        }));
        return Promise.all(promises);
    }

    async function init(canvasId, c, r, cs, ref) {
        canvas = document.getElementById(canvasId);
        ctx = canvas.getContext('2d');
        cols = c; rows = r; cellSize = cs;
        dotnetRef = ref;

        await loadSprites();
        fitCanvas();
        bindEvents();
    }

    function fitCanvas() {
        const wrap = canvas.parentElement;
        canvas.width = wrap.clientWidth;
        canvas.height = wrap.clientHeight;
        if (!userHasTransformed) fitToWindow();
        else centerGrid();
    }

    function fitToWindow() {
        if (!cols || !rows) return;
        const fitScale = Math.min(
            canvas.width  / (cols * cellSize),
            canvas.height / (rows * cellSize)
        ) * 0.97;
        scale = Math.max(0.1, fitScale);
        centerGrid();
    }

    function centerGrid() {
        tx = (canvas.width  - cols * cellSize * scale) / 2;
        ty = (canvas.height - rows * cellSize * scale) / 2;
    }

    function bindEvents() {
        canvas.addEventListener('wheel', onWheel, { passive: false });
        canvas.addEventListener('mousedown', onMouseDown);
        canvas.addEventListener('mousemove', onMouseMove);
        canvas.addEventListener('mouseup', onMouseUp);
        canvas.addEventListener('dblclick', onDblClick);
        canvas.addEventListener('click', onClick);
        canvas.addEventListener('mouseleave', onMouseLeave);
        canvas.addEventListener('contextmenu', e => e.preventDefault());
        window.addEventListener('resize', () => { fitCanvas(); scheduleRedraw(); });
        window.addEventListener('keydown', onKeyDown);
    }

    function scheduleRedraw() {
        if (isAnimating) return;
        if (rafPending) return;
        rafPending = true;
        requestAnimationFrame(() => {
            rafPending = false;
            drawFrame();
        });
    }

    function onKeyDown(e) {
        if (!dotnetRef) return;
        if (e.code === 'Space') {
            e.preventDefault();
            dotnetRef.invokeMethodAsync('OnKeyTogglePlay');
        } else if (e.code === 'KeyR') {
            dotnetRef.invokeMethodAsync('OnKeyRestart');
        }
    }

    function onWheel(e) {
        e.preventDefault();
        userHasTransformed = true;
        const rect = canvas.getBoundingClientRect();
        const mx = e.clientX - rect.left;
        const my = e.clientY - rect.top;
        const factor = e.deltaY < 0 ? 1.1 : 0.9;
        scale = Math.max(0.2, Math.min(10, scale * factor));
        tx = mx - (mx - tx) * factor;
        ty = my - (my - ty) * factor;
        scheduleRedraw();
    }

    function onMouseDown(e) {
        if (e.button === 2) {
            userHasTransformed = true;
            isPanning = true;
            panStart = { x: e.clientX - tx, y: e.clientY - ty };
            canvas.style.cursor = 'grabbing';
        }
    }

    function onMouseMove(e) {
        if (isPanning) {
            tx = e.clientX - panStart.x;
            ty = e.clientY - panStart.y;
            scheduleRedraw();
        }
        const rect = canvas.getBoundingClientRect();
        const mx = e.clientX - rect.left;
        const my = e.clientY - rect.top;
        const cell = screenToCell(e);
        if (cell && (hoveredCell === null || cell.col !== hoveredCell.col || cell.row !== hoveredCell.row)) {
            hoveredCell = cell;
            if (dotnetRef) dotnetRef.invokeMethodAsync('OnHover', cell.col, cell.row, mx, my);
        } else if (!cell && hoveredCell !== null) {
            hoveredCell = null;
            if (dotnetRef) dotnetRef.invokeMethodAsync('OnHover', -1, -1, 0, 0);
        }
    }

    function onMouseLeave() {
        hoveredCell = null;
        if (dotnetRef) dotnetRef.invokeMethodAsync('OnHover', -1, -1, 0, 0);
    }

    function onMouseUp(e) {
        if (e.button === 2) {
            isPanning = false;
            canvas.style.cursor = 'crosshair';
        }
    }

    function onDblClick(e) {
        userHasTransformed = false;
        fitToWindow();
        scheduleRedraw();
    }

    function onClick(e) {
        const cell = screenToCell(e);
        if (cell) {
            selectedCell = cell;
            scheduleRedraw();
            if (dotnetRef) dotnetRef.invokeMethodAsync('OnCellClick', cell.col, cell.row);
        }
    }

    function screenToCell(e) {
        const rect = canvas.getBoundingClientRect();
        const mx = e.clientX - rect.left;
        const my = e.clientY - rect.top;
        const col = Math.floor((mx - tx) / (cellSize * scale));
        const row = Math.floor((my - ty) / (cellSize * scale));
        if (col < 0 || col >= cols || row < 0 || row >= rows) return null;
        return { col, row };
    }

    function drawCell(px, py, cs, type, nat, nationColors, col, row) {
        const w = cs - 1;
        const nationColor = (nat >= 0 && nat < nationColors.length) ? nationColors[nat] : '#222';

        ctx.fillStyle = nationColor;
        ctx.fillRect(px, py, w, w);

        if (sprites[type]) {
            ctx.drawImage(sprites[type], px + 1, py + 1, w - 2, w - 2);
        } else {
            const inner = Math.max(2, Math.floor(cs * 0.45));
            const off = Math.floor((cs - inner) / 2);
            ctx.fillStyle = TYPE_COLORS[type] ?? '#fff';
            ctx.fillRect(px + off, py + off, inner, inner);
        }

        if (selectedCell && col >= 0 && selectedCell.col === col && selectedCell.row === row) {
            ctx.strokeStyle = '#fff';
            ctx.lineWidth = 1 / scale;
            ctx.strokeRect(px + 0.5, py + 0.5, w - 1, w - 1);
        }
    }

    function drawCellScaled(col, row, cs, type, nat, nationColors, sizeFactor) {
        if (sizeFactor <= 0) return;
        const fullW = cs - 1;
        const w = fullW * sizeFactor;
        if (w < 1) return;
        const offset = (fullW - w) / 2;
        const px = col * cs + offset;
        const py = row * cs + offset;
        const nationColor = (nat >= 0 && nat < nationColors.length) ? nationColors[nat] : '#222';
        ctx.fillStyle = nationColor;
        ctx.fillRect(px, py, w, w);
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

    function drawFrame() {
        if (!ctx) return;

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

        ctx.strokeStyle = '#999999';
        ctx.lineWidth = 2 / scale;
        ctx.strokeRect(0, 0, cols * cs, rows * cs);

        ctx.restore();
    }

    function easeInOut(t) {
        return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
    }

    function lerp(a, b, t) {
        return a + (b - a) * t;
    }

    function drawFrameAnimated(t, excludeSet, moves, births, deaths, nationColors) {
        if (!ctx) return;

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
            let px, py;
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

        ctx.strokeStyle = '#999999';
        ctx.lineWidth = 2 / scale;
        ctx.strokeRect(0, 0, cols * cs, rows * cs);

        ctx.restore();
    }

    function renderFrame(cells, nationColors, newCols, newRows, moves, births, deaths, animationEnabled, stepIntervalMs) {
        if (!ctx) return Promise.resolve();

        const gridChanged = (newCols !== cols || newRows !== rows);
        cols = newCols; rows = newRows;
        cachedCells = cells;
        cachedNationColors = nationColors;
        if (gridChanged && !userHasTransformed) fitToWindow();

        const hasMoves   = moves  && moves.length  > 0;
        const hasBirths  = births && births.length  > 0;
        const hasDeaths  = deaths && deaths.length  > 0;
        const shouldAnimate = animationEnabled && !gridChanged && (hasMoves || hasBirths || hasDeaths);

        if (shouldAnimate) {
            return new Promise(resolve => {
                const animDuration = stepIntervalMs * 0.65;
                const startTime = performance.now();

                const excludeSet = new Set();
                if (hasMoves) {
                    for (let i = 0; i < moves.length; i++) {
                        excludeSet.add(moves[i].fromCol + ',' + moves[i].fromRow);
                        excludeSet.add(moves[i].toCol  + ',' + moves[i].toRow);
                    }
                }
                if (hasBirths) {
                    for (let i = 0; i < births.length; i++)
                        excludeSet.add(births[i].col + ',' + births[i].row);
                }
                if (hasDeaths) {
                    for (let i = 0; i < deaths.length; i++)
                        excludeSet.add(deaths[i].col + ',' + deaths[i].row);
                }

                isAnimating = true;

                function frame(now) {
                    const elapsed = now - startTime;
                    const rawT = Math.min(1.0, elapsed / animDuration);
                    const t = easeInOut(rawT);

                    drawFrameAnimated(t, excludeSet, moves || [], births || [], deaths || [], nationColors);

                    if (rawT < 1.0) {
                        requestAnimationFrame(frame);
                    } else {
                        isAnimating = false;
                        drawFrame();
                        resolve();
                    }
                }

                requestAnimationFrame(frame);
            });
        } else {
            drawFrame();
            return Promise.resolve();
        }
    }

    function getCanvasSize() {
        if (!canvas) return { width: 800, height: 600 };
        return { width: canvas.width, height: canvas.height };
    }

    function updateGridSize(c, r) {
        cols = c; rows = r;
    }

    function saveSettings(json) {
        try { localStorage.setItem(SETTINGS_KEY, json); } catch (e) {}
    }

    function loadSettings() {
        try { return localStorage.getItem(SETTINGS_KEY) || null; } catch (e) { return null; }
    }

    function clearSettings() {
        try { localStorage.removeItem(SETTINGS_KEY); } catch (e) {}
    }

    return { init, renderFrame, getCanvasSize, updateGridSize, saveSettings, loadSettings, clearSettings };
})();
