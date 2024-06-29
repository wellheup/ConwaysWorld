using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static ConwaysWorld.Cell_Generator;
using System;
using UnityEngine.Assertions;

namespace ConwaysWorld
{

    public class View : MonoBehaviour
    {
        private Cell[,] _cellGrid;
        private BaseTile[,] _displayGrid;
        private float _baseTileSize, _canvasWidth, _canvasHeight, _tileScaleMod = 1;
        public bool IsRendering = false;
        private int AttemptsAtLife = 0, Generation = 0, CurrentPopulation = 0;

        [SerializeField] private BaseTile _baseTilePrefab;
        [SerializeField] private Transform _gridContainer;
        private Dictionary<E_CellType, Sprite> _cellSprites;


        public void InitiateDisplayGrid(Cell[,] cellGrid, float canvasWidth, float canvasHeight)
        {
            _canvasWidth = canvasWidth;
            _canvasHeight = canvasHeight;
            _cellGrid = cellGrid;
            _baseTileSize = _baseTilePrefab.Image.rectTransform.sizeDelta.y;
            _cellSprites = new();

            foreach (E_CellType i in Enum.GetValues(typeof(E_CellType)))
            {
                _cellSprites.Add(i, null);
                Addressables.LoadAssetAsync<Sprite>($"Assets/Sprites/{(i == E_CellType.Cell ? E_CellType.Cell_Basic : i)}.jpg").Completed +=
                    (spriteAsyncOpHandle) =>
                    {
                        if (spriteAsyncOpHandle.Status == AsyncOperationStatus.Succeeded)
                        {
                            _cellSprites[i] = spriteAsyncOpHandle.Result;
                        }
                        else
                        {
                            Debug.Log($"Failed to load {i}.jpg in View");
                        }
                    };
            }
            // revert dead sprite to basic color to mitigate busy visuals...
            Addressables.LoadAssetAsync<Sprite>($"Assets/Sprites/BlankBaseTileSprite.png").Completed +=
                    (spriteAsyncOpHandle) =>
                    {
                        if (spriteAsyncOpHandle.Status == AsyncOperationStatus.Succeeded)
                        {
                            _cellSprites[E_CellType.Cell_Dead] = spriteAsyncOpHandle.Result;
                        }
                        else
                        {
                            Debug.Log($"Failed to load BlankBaseTileSprite.png in View");
                        }
                    };

            FillDisplayGrid(false);
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
                    Destroy(_displayGrid[x, y].Image);
                    Destroy(_displayGrid[x, y].gameObject);
                }
            }
        }

        private void FillDisplayGrid(bool updateTileSize)
        {
            int cellNum = 0;
            int columns = _cellGrid.GetLength(0);
            int rows = _cellGrid.GetLength(1);

            if (updateTileSize)
                _tileScaleMod = Mathf.Min(_canvasHeight / rows, _canvasWidth / columns) / 100;

            float currentTileSize = _baseTileSize * _tileScaleMod;// scale to fit canvas, 0-1 scale
            _displayGrid = new BaseTile[columns, rows];
            _gridContainer.GetComponent<RectTransform>().anchorMin = new Vector2(0.0f, 0.0f);
            _gridContainer.GetComponent<RectTransform>().anchorMax = new Vector2(0.0f, 0.0f);

            float xOffset = (_canvasWidth - currentTileSize * columns) / 2; //split the edge padding
            float yOffset = (_canvasHeight - currentTileSize * rows) / 2;
            float xPos = xOffset;
            float yPos = yOffset;
            for (int x = 0; x < _cellGrid.GetLength(0); x++)
            {
                for (int y = 0; y < _cellGrid.GetLength(1); y++)
                {
                    BaseTile newTile = Instantiate(_baseTilePrefab, _gridContainer);
                    newTile.GetComponent<RectTransform>().localScale = new Vector3(_tileScaleMod, _tileScaleMod, _tileScaleMod);
                    newTile.transform.localPosition = new Vector3(xPos, yPos, 0);
                    newTile.name = cellNum++ + " (" + x + ", " + y + ")";
                    _displayGrid[x, y] = newTile;
                    yPos += currentTileSize;
                }
                xPos += currentTileSize;
                yPos = yOffset;
            }
        }

        public void ReSizeDisplayGrid()
        {
            DestroyOldDisplayGrid();
            FillDisplayGrid(true);
        }

        public void UpdateCellBorders(Cell cell, int x, int y)
        {
            string[] NESW_Nations = new string[] { "north", "east", "south", "west" };
            if (cell.GetIsAlive() && cell.Nationality != -1)
            {
                for (int i = 0; i < NESW_Nations.Length; i++)
                {
                    try
                    {
                        if (cell.Nationality == cell.CellNeighborhood.NeighborhoodDict[NESW_Nations[i]].Nationality)
                        {
                            _displayGrid[x, y].Borders[i].color = Color.clear;
                        }
                        else
                        {
                            _displayGrid[x, y].Borders[i].color = Cell_Nation.Nation_Colors[cell.Nationality];
                        }
                    }
                    catch
                    {
                        _displayGrid[x, y].Borders[i].color = Cell_Nation.Nation_Colors[cell.Nationality];
                    }
                }
            }
            else
            {
                for (int i = 0; i < NESW_Nations.Length; i++)
                {
                    _displayGrid[x, y].Borders[i].color = Color.clear;
                }
            }
            // try
            // {
            //     Assert.IsTrue(cell.GetIsAlive() && cell.Nationality == -1);
            // }
            // catch (Exception e)
            // {
            //     Debug.Log($"{_displayGrid[x, y].name} has no nation after spawn for some reason");
            //     // throw e;
            // }
        }

        public void RenderWorldState(Cell[,] cellGrid, int attemptsAtLife, int generation, int currentPopulation)
        {
            AttemptsAtLife = attemptsAtLife;
            Generation = generation;
            CurrentPopulation = currentPopulation;
            _cellGrid = cellGrid;
            int cellNum = 0;

            if (_cellGrid.GetLength(0) > _displayGrid.GetLength(0))
            {
                ReSizeDisplayGrid();
            }
            for (int x = 0; x < cellGrid.GetLength(0); x++)
            {
                for (int y = 0; y < cellGrid.GetLength(1); y++)
                {
                    string lifeStatus = "???";
                    _displayGrid[x, y].name = $"{cellNum++} ({x}, {y}) {lifeStatus} {_cellGrid[x, y].CellType} [{_cellGrid[x, y].Nationality}]";
                    UpdateCellBorders(_cellGrid[x, y], x, y);
                    if (_cellGrid[x, y].GetIsAlive())
                    {
                        lifeStatus = "Alive";
                        _displayGrid[x, y].name = $"{cellNum++} ({x}, {y}) {lifeStatus} {_cellGrid[x, y].CellType} [{_cellGrid[x, y].Nationality}]";
                        // _displayGrid[x, y].Image.color = _cellGrid[x, y].Nationality != -1 ? Cell_Nation.Nation_Colors[_cellGrid[x, y].Nationality] : Color.white;
                        _displayGrid[x, y].Image.sprite = _cellSprites[_cellGrid[x, y].CellType] ? _cellSprites[_cellGrid[x, y].CellType] : _cellSprites[E_CellType.Cell_Dead];
                    }
                    else
                    {
                        lifeStatus = "Dead";
                        _displayGrid[x, y].name = $"{cellNum++} ({x}, {y}) {lifeStatus} {_cellGrid[x, y].CellType} [{_cellGrid[x, y].Nationality}]";
                        // _displayGrid[x, y].Image.color = Color.white;
                        _displayGrid[x, y].Image.sprite = _cellSprites[E_CellType.Cell_Dead];
                    }
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
