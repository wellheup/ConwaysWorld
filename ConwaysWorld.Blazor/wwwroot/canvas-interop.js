window.ConwaysInterop = (() => {
    let canvas, ctx;
    let cellSize = 14;
    let cols = 0, rows = 0;
    let scale = 1, tx = 0, ty = 0;
    let isPanning = false, panStart = { x: 0, y: 0 };
    let dotnetRef = null;
    let hoveredCell = null;
    let selectedCell = null;

    // Cached cell data for redraws during pan/zoom while paused
    let cachedCells = [];
    let cachedNationColors = [];
    let rafPending = false;

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
        scale = 1;
        centerGrid();
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

    function renderFrame(cells, nationColors, newCols, newRows) {
        if (!ctx) return;
        cols = newCols; rows = newRows;
        cachedCells = cells;
        cachedNationColors = nationColors;
        drawFrame();
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

            const px = c.col * cs;
            const py = c.row * cs;
            const w = cs - 1;

            const nationColor = (c.nat >= 0 && c.nat < nationColors.length)
                ? nationColors[c.nat] : '#222';

            ctx.fillStyle = nationColor;
            ctx.fillRect(px, py, w, w);

            if (sprites[c.type]) {
                ctx.drawImage(sprites[c.type], px + 1, py + 1, w - 2, w - 2);
            } else {
                const inner = Math.max(2, Math.floor(cs * 0.45));
                const off = Math.floor((cs - inner) / 2);
                ctx.fillStyle = TYPE_COLORS[c.type] ?? '#fff';
                ctx.fillRect(px + off, py + off, inner, inner);
            }

            if (selectedCell && selectedCell.col === c.col && selectedCell.row === c.row) {
                ctx.strokeStyle = '#fff';
                ctx.lineWidth = 1 / scale;
                ctx.strokeRect(px + 0.5, py + 0.5, w - 1, w - 1);
            }
        }

        ctx.restore();
    }

    function getCanvasSize() {
        if (!canvas) return { width: 800, height: 600 };
        return { width: canvas.width, height: canvas.height };
    }

    function updateGridSize(c, r) {
        cols = c; rows = r;
    }

    return { init, renderFrame, getCanvasSize, updateGridSize };
})();
