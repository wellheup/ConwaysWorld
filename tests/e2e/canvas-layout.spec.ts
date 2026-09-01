import { expect, Page, test } from '@playwright/test';

type CanvasViewport = {
    grid: { width: number; height: number };
    scale: number;
    tx: number;
    ty: number;
    userHasTransformed: boolean;
};

type Rect = { x: number; y: number; width: number; height: number };

type CanvasLayout = {
    viewport: CanvasViewport;
    canvas: Rect;
    toolbar: Rect;
    sidebar: Rect;
};

const FIT_PADDING = 0.97;

const paneStates = [
    { name: 'both panes open', toolbar: true, sidebar: true },
    { name: 'toolbar collapsed', toolbar: false, sidebar: true },
    { name: 'sidebar collapsed', toolbar: true, sidebar: false },
    { name: 'both panes collapsed', toolbar: false, sidebar: false },
];

const viewports = [
    { name: 'desktop', width: 1280, height: 900 },
    { name: 'narrow', width: 640, height: 900 },
];

async function readViewport(page: Page): Promise<CanvasViewport> {
    return page.evaluate(() => {
        const interop = (window as typeof window & {
            ConwaysInterop: { getCanvasViewport: () => CanvasViewport };
        }).ConwaysInterop;
        return interop.getCanvasViewport();
    });
}

async function waitForFit(page: Page): Promise<CanvasViewport> {
    await page.waitForFunction(() => {
        const interop = (window as typeof window & {
            ConwaysInterop?: { getCanvasViewport?: () => CanvasViewport };
        }).ConwaysInterop;
        if (!interop?.getCanvasViewport) return false;
        const state = interop.getCanvasViewport();
        return state.grid.width > 0 && state.grid.height > 0 && state.scale > 0;
    });

    await page.waitForTimeout(350);
    const state = await readViewport(page);
    expect(state.userHasTransformed).toBe(false);
    return state;
}

async function setPaneState(page: Page, toolbar: boolean, sidebar: boolean): Promise<void> {
    const toolbarIsOpen = await page.locator('.cw-toolbar').evaluate(element => !element.classList.contains('cw-collapsed'));
    if (toolbarIsOpen !== toolbar) await page.locator('.cw-toolbar-toggle').click();

    const sidebarIsOpen = await page.locator('.cw-sidebar').evaluate(element => !element.classList.contains('cw-collapsed'));
    if (sidebarIsOpen !== sidebar) await page.locator('.cw-sidebar-edge-tab').click();

    await page.waitForTimeout(350);
}

async function readLayout(page: Page): Promise<CanvasLayout> {
    const [viewport, canvas, toolbar, sidebar] = await Promise.all([
        readViewport(page),
        page.locator('#sim-canvas').boundingBox(),
        page.locator('.cw-toolbar').boundingBox(),
        page.locator('.cw-sidebar').boundingBox(),
    ]);

    expect(canvas).not.toBeNull();
    expect(toolbar).not.toBeNull();
    expect(sidebar).not.toBeNull();
    return {
        viewport,
        canvas: canvas!,
        toolbar: toolbar!,
        sidebar: sidebar!,
    };
}

function rectanglesOverlap(a: Rect, b: Rect): boolean {
    return (
        a.x < b.x + b.width &&
        a.x + a.width > b.x &&
        a.y < b.y + b.height &&
        a.y + a.height > b.y
    );
}

function expectGridInsideVisibleArea(layout: CanvasLayout): void {
    const { viewport, canvas, toolbar, sidebar } = layout;
    const canvasRight = canvas.x + canvas.width;
    const canvasBottom = canvas.y + canvas.height;
    const visibleTop = rectanglesOverlap(canvas, toolbar) ? Math.max(canvas.y, toolbar.y + toolbar.height) : canvas.y;
    const visibleRight = rectanglesOverlap(canvas, sidebar) ? Math.min(canvasRight, sidebar.x) : canvasRight;
    const visibleWidth = visibleRight - canvas.x;
    const visibleHeight = canvasBottom - visibleTop;
    const expectedScale =
        Math.min(visibleWidth / viewport.grid.width, visibleHeight / viewport.grid.height) * FIT_PADDING;
    const expectedGridLeft = canvas.x + (visibleWidth - viewport.grid.width * expectedScale) / 2;
    const expectedGridTop = visibleTop + (visibleHeight - viewport.grid.height * expectedScale) / 2;
    const gridLeft = canvas.x + viewport.tx;
    const gridTop = canvas.y + viewport.ty;
    const gridRight = gridLeft + viewport.grid.width * viewport.scale;
    const gridBottom = gridTop + viewport.grid.height * viewport.scale;
    const tolerance = 1;

    expect(viewport.scale).toBeCloseTo(expectedScale, 5);
    expect(gridLeft).toBeCloseTo(expectedGridLeft, 4);
    expect(gridTop).toBeCloseTo(expectedGridTop, 4);
    expect(gridLeft).toBeGreaterThanOrEqual(canvas.x - tolerance);
    expect(gridTop).toBeGreaterThanOrEqual(visibleTop - tolerance);
    expect(gridRight).toBeLessThanOrEqual(visibleRight + tolerance);
    expect(gridBottom).toBeLessThanOrEqual(canvasBottom + tolerance);
}

async function dispatchWheel(page: Page, deltaY: number): Promise<void> {
    await page.locator('#sim-canvas').evaluate((element, wheelDelta) => {
        const canvas = element as HTMLCanvasElement;
        const rect = canvas.getBoundingClientRect();
        canvas.dispatchEvent(
            new WheelEvent('wheel', {
                bubbles: true,
                cancelable: true,
                clientX: rect.left + rect.width / 2,
                clientY: rect.top + rect.height / 2,
                deltaY: wheelDelta,
            }),
        );
    }, deltaY);
    await page.waitForTimeout(50);
}

for (const viewport of viewports) {
    test(`${viewport.name} pane transitions keep the fitted field unobscured`, async ({ page }) => {
        await page.setViewportSize({ width: viewport.width, height: viewport.height });
        await page.goto('/');
        await waitForFit(page);

        for (const paneState of paneStates) {
            await setPaneState(page, paneState.toolbar, paneState.sidebar);
            await waitForFit(page);

            await test.step(`${paneState.name} keeps the whole field inside the visible canvas`, async () => {
                expectGridInsideVisibleArea(await readLayout(page));
            });

            await test.step(`${paneState.name} wheel zoom-out returns to fitted bounds`, async () => {
                await dispatchWheel(page, -100);
                expect((await readViewport(page)).userHasTransformed).toBe(true);
                await dispatchWheel(page, 100);
                await waitForFit(page);
                expectGridInsideVisibleArea(await readLayout(page));
            });

            await test.step(`${paneState.name} double-click reset returns to fitted bounds`, async () => {
                await dispatchWheel(page, -100);
                expect((await readViewport(page)).userHasTransformed).toBe(true);
                await page.locator('#sim-canvas').dblclick({ position: { x: 20, y: 200 } });
                await waitForFit(page);
                expectGridInsideVisibleArea(await readLayout(page));
            });
        }
    });
}