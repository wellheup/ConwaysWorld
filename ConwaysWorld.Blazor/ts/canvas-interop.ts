// ── Canvas interop: init, render, and public API ──────────────────────────────
// Depends on canvas-types.ts, canvas-state.ts, canvas-draw.ts, canvas-events.ts
// compiled before this file (see tsconfig.json "files" array).

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

function fitCanvas(): void {
    if (!canvas) return;
    const wrap = canvas.parentElement!;
    canvas.width = wrap.clientWidth;
    canvas.height = wrap.clientHeight;
    if (!userHasTransformed) fitToWindow();
    else centerGrid();
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
            for (let i = 0; i < coronations.length; i++) excludeSet.add(coronations[i].col + ',' + coronations[i].row);
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

function getCanvasViewport(): {
    grid: { width: number; height: number };
    scale: number;
    tx: number;
    ty: number;
    userHasTransformed: boolean;
} {
    return {
        grid: {
            width: cols * cellSize,
            height: rows * cellSize,
        },
        scale,
        tx,
        ty,
        userHasTransformed,
    };
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

function watchToolbarHeight(): void {
    const toolbar = document.querySelector('.cw-toolbar') as HTMLElement | null;
    const sidebar = document.querySelector('.cw-sidebar') as HTMLElement | null;
    let updatePending = false;

    const update = () => {
        if (toolbar) {
            const toolbarHeight = toolbar.getBoundingClientRect().height;
            document.documentElement.style.setProperty('--toolbar-h', `${toolbarHeight}px`);
        }
        if (canvas) {
            if (!userHasTransformed) fitToWindow();
            else centerGrid();
            scheduleRedraw();
        }
    };

    // Queue measurement after a class change so fixed-pane transitions are
    // measured at their rendered size. ResizeObserver then tracks each frame
    // of the transition and content-driven height changes.
    const queueUpdate = () => {
        if (updatePending) return;
        updatePending = true;
        requestAnimationFrame(() => {
            updatePending = false;
            update();
        });
    };

    const resizeObserver = new ResizeObserver(queueUpdate);
    if (toolbar) resizeObserver.observe(toolbar);
    if (sidebar) resizeObserver.observe(sidebar);

    const mutationObserver = new MutationObserver(queueUpdate);
    if (toolbar) mutationObserver.observe(toolbar, { attributes: true, attributeFilter: ['class'] });
    if (sidebar) mutationObserver.observe(sidebar, { attributes: true, attributeFilter: ['class'] });

    update();
}

// ── Public API ────────────────────────────────────────────────────────────────
(window as any).ConwaysInterop = {
    init,
    renderFrame,
    getCanvasSize,
    getCanvasViewport,
    updateGridSize,
    saveSettings,
    loadSettings,
    clearSettings,
    toggleFullscreen,
    setEditMode,
    watchToolbarHeight,
};
