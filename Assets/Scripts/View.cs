using UnityEngine;
using UnityEngine.UIElements;
namespace ConwaysWorld
{

    public class View : MonoBehaviour
    {
        private Cell[,] _cellGrid;
        private BaseTile[,] _displayGrid;
        private float _baseTileSize, _canvasWidth, _canvasHeight;
        public bool IsRendering = false;
        private int AttemptsAtLife = 0, Generation = 0, CurrentPopulation = 0;

        [SerializeField] private BaseTile _baseTilePrefab;
        [SerializeField] private Transform _gridContainer;

        public void InitiateDisplayGrid(Cell[,] cellGrid, float canvasWidth, float canvasHeight)
        {
            _canvasWidth = canvasWidth;
            _canvasHeight = canvasHeight;
            _cellGrid = cellGrid;
            FillDisplayGrid();
        }

        public void PrintWorldStats(int attemptsAtLife, int generation, int currentPopulation)
        {
            print(
                "Attempt at Life: "
                    + attemptsAtLife
                    + "    Generation: "
                    + generation
                    + "    Current Population: "
                    + currentPopulation
            );
        }

        private void DestroyOldDisplayGrid()
        {
            for (int x = 0; x < _displayGrid.GetLength(0); x++)
            {
                for (int y = 0; y < _displayGrid.GetLength(1); y++)
                {
                    Destroy(_displayGrid[x, y]);
                }
            }
        }

        private void FillDisplayGrid()
        {
            int cellNum = 0;
            _baseTileSize = _baseTilePrefab.Image.rectTransform.sizeDelta.y * _baseTilePrefab.Image.rectTransform.localScale.y;
            int columns = _cellGrid.GetLength(0);
            int rows = _cellGrid.GetLength(1);
            _displayGrid = new BaseTile[columns, rows];
            _gridContainer.GetComponent<RectTransform>().anchorMin = new Vector2(0.0f, 0.0f);
            _gridContainer.GetComponent<RectTransform>().anchorMax = new Vector2(0.0f, 0.0f);

            float xOffset = (_canvasWidth - _baseTileSize * columns) / 2;
            float yOffset = (_canvasHeight - _baseTileSize * rows) / 2;
            float xPos = xOffset;
            float yPos = yOffset;
            for (int x = 0; x < _cellGrid.GetLength(0); x++)
            {
                for (int y = 0; y < _cellGrid.GetLength(1); y++)
                {
                    BaseTile newTile = Instantiate(_baseTilePrefab, _gridContainer);
                    newTile.transform.localPosition = new Vector3(xPos, yPos, 0);
                    newTile.name = cellNum++ + " (" + x + ", " + y + ")";
                    _displayGrid[x, y] = newTile;
                    yPos += _baseTileSize;
                }
                xPos += _baseTileSize;
                yPos = yOffset;
            }
        }

        public void ReSizeDisplayGrid()
        {
            DestroyOldDisplayGrid();
            FillDisplayGrid();
        }

        public void RenderWorldState(Cell[,] cellGrid, int attemptsAtLife, int generation, int currentPopulation)
        {
            AttemptsAtLife = attemptsAtLife;
            Generation = generation;
            CurrentPopulation = currentPopulation;
            _cellGrid = cellGrid;

            if (_cellGrid.GetLength(0) > _displayGrid.GetLength(0))
            {
                ReSizeDisplayGrid();
            }
            for (int x = 0; x < cellGrid.GetLength(0); x++)
            {
                for (int y = 0; y < cellGrid.GetLength(1); y++)
                {
                    _displayGrid[x, y].Image.color = _cellGrid[x, y].GetCurrentColor();
                }
            }
            // PrintWorldStats(AttemptsAtLife, Generation, CurrentPopulation);
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
