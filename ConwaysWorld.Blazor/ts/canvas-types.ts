// ── Type declarations ─────────────────────────────────────────────────────────
// Compiled as part of the outFile bundle — no import/export.

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
