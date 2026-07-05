// ── Shared canvas state ───────────────────────────────────────────────────────
// Global variables shared across canvas-draw, canvas-events, and canvas-interop.

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

// ── Edit mode state ───────────────────────────────────────────────────────────
let editMode = false;
let editMoveMode = false;
let editButtonDown = false;
let editEraseButtonDown = false;
let editMoveSelected: GridCell | null = null;
let editMoveWasSelectedBeforeMouseDown = false;

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
