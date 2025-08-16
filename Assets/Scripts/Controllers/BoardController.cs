using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public enum eAutoMode
    {
        OFF,
        AUTO_WIN,
        AUTO_LOSE
    }

    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private bool m_isDragging;

    private bool m_isProcessing = false;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;

    private bool m_isTimeAttackMode = false;

    private float m_autoPlayDelay = 0.5f;

    private Coroutine m_autoPlayCoroutine = null;

    private eAutoMode m_autoMode = eAutoMode.OFF;

    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        Fill();
    }

    private void Fill()
    {
        m_board.Fill();
        // FindMatchesAndCollapse();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                // StopHints();
                SetAutoMode(eAutoMode.OFF);
                break;
        }
    }

    public void SetTimeAttackMode(bool enabled)
    {
        m_isTimeAttackMode = enabled;
    }
    
    public void SetAutoWin(bool enabled)
    {
        SetAutoMode(enabled ? eAutoMode.AUTO_WIN : eAutoMode.OFF);
    }

    public void SetAutoLose(bool enabled)
    {
        SetAutoMode(enabled ? eAutoMode.AUTO_LOSE : eAutoMode.OFF);
    }

    public void SetAutoMode(eAutoMode mode)
    {
        if (m_autoMode == mode) return;

        m_autoMode = mode;

        if (m_autoPlayCoroutine != null)
        {
            StopCoroutine(m_autoPlayCoroutine);
            m_autoPlayCoroutine = null;
        }

        if (mode != eAutoMode.OFF)
        {
            StartCoroutine(StartAutoAfterDelay(1f));
        }
    }

    private IEnumerator StartAutoAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        m_autoPlayCoroutine = StartCoroutine(AutoPlayCoroutine());
    }

    private IEnumerator AutoPlayCoroutine()
    {
        while (m_autoMode != eAutoMode.OFF && !m_gameOver)
        {
            yield return new WaitUntil(() => !IsBusy && !m_isProcessing);

            Cell cellToMove = GetNextAutoCell();
            if (cellToMove != null)
            {
                OnCellTapped(cellToMove);
                yield return new WaitForSeconds(m_autoPlayDelay);
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }

        m_autoPlayCoroutine = null;
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;
        if (m_isProcessing) return;

        HandleTapInput();

        // if (!m_hintIsShown)
        // {
        //     m_timeAfterFill += Time.deltaTime;
        //     if (m_timeAfterFill > m_gameSettings.TimeForHint)
        //     {
        //         m_timeAfterFill = 0f;
        //         ShowHint();
        //     }
        // }

        // if (Input.GetMouseButtonDown(0))
        // {
        //     var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        //     if (hit.collider != null)
        //     {
        //         m_isDragging = true;
        //         m_hitCollider = hit.collider;
        //     }
        // }

        // if (Input.GetMouseButtonUp(0))
        // {
        //     ResetRayCast();
        // }

        // if (Input.GetMouseButton(0) && m_isDragging)
        // {
        //     var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        //     if (hit.collider != null)
        //     {
        //         if (m_hitCollider != null && m_hitCollider != hit.collider)
        //         {
        //             StopHints();

        //             Cell c1 = m_hitCollider.GetComponent<Cell>();
        //             Cell c2 = hit.collider.GetComponent<Cell>();
        //             if (AreItemsNeighbor(c1, c2))
        //             {
        //                 IsBusy = true;
        //                 SetSortingLayer(c1, c2);
        //                 m_board.Swap(c1, c2, () =>
        //                 {
        //                     FindMatchesAndCollapse(c1, c2);
        //                 });

        //                 ResetRayCast();
        //             }
        //         }
        //     }
        //     else
        //     {
        //         ResetRayCast();
        //     }
        // }
    }

    private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    private void HandleTapInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                Cell tappedCell = hit.collider.GetComponent<Cell>();
                if (tappedCell != null && !tappedCell.IsEmpty)
                {
                    OnCellTapped(tappedCell);
                }
            }
        }
    }

    private bool IsBottomCell(Cell cell)
    {
        return cell.BoardY == -1;
    }

    private void OnCellTapped(Cell tappedCell)
    {
        if (m_isProcessing) return;
        if (tappedCell.IsEmpty) return;

        if (m_isTimeAttackMode && IsBottomCell(tappedCell))
        {
            ReturnItemToBoard(tappedCell);
            return;
        }

        if (!IsBottomCell(tappedCell) && m_board.CanMoveItemToBottom(tappedCell))
        {
            m_isProcessing = true;

            m_board.MoveItemToBottom(tappedCell, () =>
            {
                CheckWinLoseConditions();
                m_isProcessing = false;
            });
        }
        else
        {
            Debug.Log("Cannot move item to bottom");
        }
    }

    private void CheckWinLoseConditions()
    {
        if (m_isTimeAttackMode)
        {
            if (m_board.IsBoardEmpty())
            {
                m_gameManager.WinGame();
            }
        }
        else
        {
            if (m_board.AreBottomCellsFull())
            {
                m_gameManager.GameOver();
            }
            else if (m_board.IsBoardEmpty())
            {
                m_gameManager.WinGame();
            }
        }
    }

    private void ReturnItemToBoard(Cell bottomCell)
    {
        if (bottomCell.IsEmpty) return;

        m_isProcessing = true;

        Item item = bottomCell.Item;
        Cell originalCell = item.OriginalCell;

        if (originalCell != null && originalCell.IsEmpty)
        {
            Vector3 startPos = bottomCell.transform.position;

            bottomCell.Free();
            originalCell.Assign(item);
            item.SetViewPosition(startPos);

            item.OriginalCell = null;

            originalCell.ApplyItemMoveToPosition(() =>
            {
                m_isProcessing = false;
            });
        }
        else
        {
            Debug.LogWarning("Original cell is not empty or not set, cannot return item to board.");
            m_isProcessing = false;
        }
    }

    private Cell GetNextAutoCell()
    {
        switch (m_autoMode)
        {
            case eAutoMode.AUTO_WIN:
                return FindBestCellToMove();
            case eAutoMode.AUTO_LOSE:
                return FindWorstCellToMove();
            default:
                Debug.LogWarning("Auto mode is OFF, no cell to move.");
                return null;
        }
    }

    private Cell FindWorstCellToMove()
    {
        Cell[] bottomCells = m_board.GetBottomCells();

        // Get all unique item types in the bottom cells
        HashSet<NormalItem.eNormalType> bottomTypes = new HashSet<NormalItem.eNormalType>();
        foreach (Cell bottomCell in bottomCells)
        {
            if (!bottomCell.IsEmpty && bottomCell.Item is NormalItem bottomItem)
            {
                bottomTypes.Add(bottomItem.ItemType);
            }
        }

        if (bottomTypes.Count > 0)
        {
            NormalItem.eNormalType type = Utils.GetRandomNormalTypeExcept(bottomTypes.ToArray());
            Cell worstCell = FindCellWithType(type);
            if (worstCell != null)
            {
                return worstCell;
            }
        }

        return FindRandomMovableCell();

    }

    private Cell FindBestCellToMove()
    {
        Cell[] bottomCells = m_board.GetBottomCells();

        foreach (Cell bottomCell in bottomCells)
        {
            if (!bottomCell.IsEmpty && bottomCell.Item is NormalItem bottomItem)
            {
                Cell sameTypeCell = FindCellWithType(bottomItem.ItemType);
                if (sameTypeCell != null)
                {
                    // Found a cell with the same item type
                    return sameTypeCell;
                }
            }
        }
        return FindRandomMovableCell();
    }

    private Cell FindCellWithType(NormalItem.eNormalType targetType)
    {
        Cell[,] boardCells = m_board.GetBoardCells();

        foreach (Cell cell in boardCells)
        {
            if (cell != null && !cell.IsEmpty && cell.Item is NormalItem item && item.ItemType == targetType)
            {
                if (m_board.CanMoveItemToBottom(cell))
                {
                    // Found a cell with the same item type that can be moved to the bottom
                    return cell;
                }
            }
        }
        return null;
    }

    private Cell FindRandomMovableCell()
    {
        Cell[,] boardCells = m_board.GetBoardCells();

        foreach (Cell cell in boardCells)
        {
            if (cell != null && !cell.IsEmpty)
            {
                if (m_board.CanMoveItemToBottom(cell))
                {
                    return cell;
                }
            }
        }
        return null;
    }

    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        if (cell1.Item is BonusItem)
        {
            cell1.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else if (cell2.Item is BonusItem)
        {
            cell2.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else
        {
            List<Cell> cells1 = GetMatches(cell1);
            List<Cell> cells2 = GetMatches(cell2);

            List<Cell> matches = new List<Cell>();
            matches.AddRange(cells1);
            matches.AddRange(cells2);
            matches = matches.Distinct().ToList();

            if (matches.Count < m_gameSettings.MatchesMin)
            {
                m_board.Swap(cell1, cell2, () =>
                {
                    IsBusy = false;
                });
            }
            else
            {
                OnMoveEvent();

                CollapseMatches(matches, cell2);
            }
        }
    }

    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if(matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        m_board.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }

    internal void Clear()
    {
        SetAutoMode(eAutoMode.OFF);
        m_board.Clear();
    }

    private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
        {
            cell.StopHintAnimation();
        }

        m_potentialMatch.Clear();
    }
}
