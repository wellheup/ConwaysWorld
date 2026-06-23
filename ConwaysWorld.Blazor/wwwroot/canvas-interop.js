"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
window.ConwaysInterop = (() => {
    let canvas = null;
    let ctx = null;
    let cellSize = 14;
    let cols = 0, rows = 0;
    let scale = 1, tx = 0, ty = 0;
    let userHasTransformed = false;
    let isPanning = false;
    let panStart = { x: 0, y: 0 };
    let dotnetRef = null;
    let hoveredCell = null;
    let selectedCell = null;
    let isAnimating = false;
    let cachedCells = [];
    let cachedNationColors = [];
    let cachedFamine = { active: false, quadrant: 0 };
    let cachedFlood = { active: false };
    let rafPending = false;
    const SETTINGS_KEY = 'cw_settings';
    const SPRITE_NAMES = [
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
        'Irradiated',
        'PlagueRat',
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
        25: '#55ff00',
        26: '#8b2020',
    };
    const sprites = {};
    function loadSprites() {
        const promises = SPRITE_NAMES.map((name, i) => new Promise(resolve => {
            const img = new Image();
            img.onload = () => {
                sprites[i] = img;
                resolve();
            };
            img.onerror = () => resolve();
            img.src = `Assets/Sprites/Cell_${name}.jpg`;
        }));
        return Promise.all(promises);
    }
    function init(canvasId, c, r, cs, ref) {
        return __awaiter(this, void 0, void 0, function* () {
            canvas = document.getElementById(canvasId);
            ctx = canvas.getContext('2d');
            cols = c;
            rows = r;
            cellSize = cs;
            dotnetRef = ref;
            yield loadSprites();
            fitCanvas();
            bindEvents();
        });
    }
    function fitCanvas() {
        if (!canvas)
            return;
        const wrap = canvas.parentElement;
        canvas.width = wrap.clientWidth;
        canvas.height = wrap.clientHeight;
        if (!userHasTransformed)
            fitToWindow();
        else
            centerGrid();
    }
    function fitToWindow() {
        if (!canvas || !cols || !rows)
            return;
        const fitScale = Math.min(canvas.width / (cols * cellSize), canvas.height / (rows * cellSize)) * 0.97;
        scale = Math.max(0.1, fitScale);
        centerGrid();
    }
    function centerGrid() {
        if (!canvas)
            return;
        tx = (canvas.width - cols * cellSize * scale) / 2;
        ty = (canvas.height - rows * cellSize * scale) / 2;
    }
    function bindEvents() {
        if (!canvas)
            return;
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
    }
    function scheduleRedraw() {
        if (isAnimating)
            return;
        if (rafPending)
            return;
        rafPending = true;
        requestAnimationFrame(() => {
            rafPending = false;
            drawFrame();
        });
    }
    function onKeyDown(e) {
        if (!dotnetRef)
            return;
        if (e.code === 'Space') {
            e.preventDefault();
            dotnetRef.invokeMethodAsync('OnKeyTogglePlay');
        }
        else if (e.code === 'KeyR') {
            dotnetRef.invokeMethodAsync('OnKeyRestart');
        }
    }
    function onWheel(e) {
        e.preventDefault();
        if (!canvas)
            return;
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
        if (!canvas)
            return;
        const rect = canvas.getBoundingClientRect();
        const mx = e.clientX - rect.left;
        const my = e.clientY - rect.top;
        const cell = screenToCell(e);
        if (cell && (hoveredCell === null || cell.col !== hoveredCell.col || cell.row !== hoveredCell.row)) {
            hoveredCell = cell;
            if (dotnetRef)
                dotnetRef.invokeMethodAsync('OnHover', cell.col, cell.row, mx, my);
        }
        else if (!cell && hoveredCell !== null) {
            hoveredCell = null;
            if (dotnetRef)
                dotnetRef.invokeMethodAsync('OnHover', -1, -1, 0, 0);
        }
    }
    function onMouseLeave() {
        hoveredCell = null;
        if (dotnetRef)
            dotnetRef.invokeMethodAsync('OnHover', -1, -1, 0, 0);
    }
    function onMouseUp(e) {
        if (e.button === 2) {
            isPanning = false;
            canvas.style.cursor = 'crosshair';
        }
    }
    function onDblClick() {
        userHasTransformed = false;
        fitToWindow();
        scheduleRedraw();
    }
    function onClick(e) {
        const cell = screenToCell(e);
        if (cell) {
            selectedCell = cell;
            scheduleRedraw();
            if (dotnetRef)
                dotnetRef.invokeMethodAsync('OnCellClick', cell.col, cell.row);
        }
    }
    function screenToCell(e) {
        if (!canvas)
            return null;
        const rect = canvas.getBoundingClientRect();
        const mx = e.clientX - rect.left;
        const my = e.clientY - rect.top;
        const col = Math.floor((mx - tx) / (cellSize * scale));
        const row = Math.floor((my - ty) / (cellSize * scale));
        if (col < 0 || col >= cols || row < 0 || row >= rows)
            return null;
        return { col, row };
    }
    function drawCell(px, py, cs, type, nat, nationColors, col, row) {
        var _a;
        if (!ctx)
            return;
        const w = cs - 1;
        const nationColor = nat >= 0 && nat < nationColors.length ? nationColors[nat] : '#222';
        ctx.fillStyle = nationColor;
        ctx.fillRect(px, py, w, w);
        if (cs * scale > 20) {
            if (sprites[type]) {
                ctx.drawImage(sprites[type], px + 1, py + 1, w - 2, w - 2);
            }
            else {
                const inner = Math.max(2, Math.floor(cs * 0.45));
                const off = Math.floor((cs - inner) / 2);
                ctx.fillStyle = (_a = TYPE_COLORS[type]) !== null && _a !== void 0 ? _a : '#fff';
                ctx.fillRect(px + off, py + off, inner, inner);
            }
        }
        if (selectedCell && col >= 0 && selectedCell.col === col && selectedCell.row === row) {
            ctx.strokeStyle = '#fff';
            ctx.lineWidth = 1 / scale;
            ctx.strokeRect(px + 0.5, py + 0.5, w - 1, w - 1);
        }
    }
    function drawCellScaled(col, row, cs, type, nat, nationColors, sizeFactor) {
        var _a;
        if (!ctx || sizeFactor <= 0)
            return;
        const fullW = cs - 1;
        const w = fullW * sizeFactor;
        if (w < 1)
            return;
        const offset = (fullW - w) / 2;
        const px = col * cs + offset;
        const py = row * cs + offset;
        const nationColor = nat >= 0 && nat < nationColors.length ? nationColors[nat] : '#222';
        ctx.fillStyle = nationColor;
        ctx.fillRect(px, py, w, w);
        if (w * scale > 20) {
            if (sprites[type]) {
                const inner = Math.max(1, w - 2);
                ctx.drawImage(sprites[type], px + 1, py + 1, inner, inner);
            }
            else {
                const inner = Math.max(1, cs * 0.45 * sizeFactor);
                const innerOff = (w - inner) / 2;
                ctx.fillStyle = (_a = TYPE_COLORS[type]) !== null && _a !== void 0 ? _a : '#fff';
                ctx.fillRect(px + innerOff, py + innerOff, inner, inner);
            }
        }
    }
    function drawCellScaledRotated(col, row, cs, type, nat, nationColors, sizeFactor, angleDeg) {
        if (!ctx || sizeFactor <= 0)
            return;
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
    function drawFloodOverlay() {
        if (!ctx || !cachedFlood.active)
            return;
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
    function drawFamineOverlay() {
        if (!ctx || !cachedFamine.active)
            return;
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
    function drawFrame() {
        if (!ctx || !canvas)
            return;
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
            if (!c.alive)
                continue;
            drawCell(c.col * cs, c.row * cs, cs, c.type, c.nat, nationColors, c.col, c.row);
        }
        drawFamineOverlay();
        drawFloodOverlay();
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
    function drawFrameAnimated(t, excludeSet, moves, births, deaths, epicDeaths, coronations, nationColors) {
        if (!ctx || !canvas)
            return;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.save();
        ctx.translate(tx, ty);
        ctx.scale(scale, scale);
        const cs = cellSize;
        for (let i = 0; i < cachedCells.length; i++) {
            const c = cachedCells[i];
            if (!c.alive)
                continue;
            if (excludeSet.has(c.col + ',' + c.row))
                continue;
            drawCell(c.col * cs, c.row * cs, cs, c.type, c.nat, nationColors, c.col, c.row);
        }
        for (let i = 0; i < moves.length; i++) {
            const m = moves[i];
            const isWrapped = Math.abs(m.fromCol - m.toCol) > 1 || Math.abs(m.fromRow - m.toRow) > 1;
            let px, py;
            if (isWrapped) {
                px = m.toCol * cs;
                py = m.toRow * cs;
            }
            else {
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
            let sf;
            if (t < 0.3) {
                sf = 1 + (t / 0.3) * 0.55;
            }
            else {
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
    function renderFrame(cells, nationColors, newCols, newRows, moves, births, deaths, epicDeaths, coronations, animationEnabled, stepIntervalMs, famine, flood) {
        if (!ctx)
            return Promise.resolve();
        const gridChanged = newCols !== cols || newRows !== rows;
        cols = newCols;
        rows = newRows;
        cachedCells = cells;
        cachedNationColors = nationColors;
        cachedFamine = famine !== null && famine !== void 0 ? famine : { active: false, quadrant: 0 };
        cachedFlood = flood !== null && flood !== void 0 ? flood : { active: false };
        let doZoom = false;
        let fromScale = 0, fromTx = 0, fromTy = 0;
        let toScale = 0, toTx = 0, toTy = 0;
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
        }
        else if (gridChanged) {
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
        const excludeSet = new Set();
        if (doCellAnim) {
            if (hasMoves) {
                for (let i = 0; i < moves.length; i++) {
                    excludeSet.add(moves[i].fromCol + ',' + moves[i].fromRow);
                    excludeSet.add(moves[i].toCol + ',' + moves[i].toRow);
                }
            }
            if (hasBirths)
                for (let i = 0; i < births.length; i++)
                    excludeSet.add(births[i].col + ',' + births[i].row);
            if (hasDeaths)
                for (let i = 0; i < deaths.length; i++)
                    excludeSet.add(deaths[i].col + ',' + deaths[i].row);
            if (hasEpicDeaths)
                for (let i = 0; i < epicDeaths.length; i++)
                    excludeSet.add(epicDeaths[i].col + ',' + epicDeaths[i].row);
            if (hasCoronations)
                for (let i = 0; i < coronations.length; i++)
                    excludeSet.add(coronations[i].col + ',' + coronations[i].row);
        }
        return new Promise(resolve => {
            const startTime = performance.now();
            isAnimating = true;
            function frame(now) {
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
                }
                else {
                    drawFrame();
                }
                if (elapsed < totalDuration) {
                    requestAnimationFrame(frame);
                }
                else {
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
    function getCanvasSize() {
        if (!canvas)
            return { width: 800, height: 600 };
        return { width: canvas.width, height: canvas.height };
    }
    function updateGridSize(c, r) {
        cols = c;
        rows = r;
    }
    function saveSettings(json) {
        try {
            localStorage.setItem(SETTINGS_KEY, json);
        }
        catch (_e) { }
    }
    function loadSettings() {
        try {
            return localStorage.getItem(SETTINGS_KEY) || null;
        }
        catch (_e) {
            return null;
        }
    }
    function clearSettings() {
        try {
            localStorage.removeItem(SETTINGS_KEY);
        }
        catch (_e) { }
    }
    return { init, renderFrame, getCanvasSize, updateGridSize, saveSettings, loadSettings, clearSettings };
})();
