# 🛠️ Unity version: 2020.3.38f

## 🎨 Task 1: Re-skin
- Replaced each item prefab's sprite with a fish sprite  
- Changed **PPU** to `110`  

---

## 🎮 Task 2: Change the Gameplay
- **Bottom Cells Implementation:**  
  Created a row of **5 cells** below the main board to hold items  
- **Input System Modification:**  
  Changed input from **swipe** to **tap** for moving items to bottom cells  
  *(bottom cells can't be tapped)*  
- **Bottom Row Match Logic:**  
  Automatically clear **3 matching items** in bottom cells  
- **Win/Lose Conditions:**  
  - **Win:** when the board is cleared → show **Win Panel**  
  - **Lose:** when bottom cells are full → show **Game Over Panel**  
- **Balanced Item Distribution:**  
  - Distribute items equally among all types with counts divisible by **3**  
    ```csharp
    base = (total // 3) // typeCount * 3
    ```
  - Handle remaining cells by creating additional groups of **3 matching items**  
    ```csharp
    additionalStack = (total - base * typeCount) / 3
    ```
- **AutoPlay Mode:**  
  Find items matching those already in the bottom row and automatically select & move them via **Coroutine**  
- **AutoLose Mode:**  
  Modified the **AutoPlay strategy** to intentionally lose the game by selecting items different from the bottom row  

---

## 🚀 Task 3: Improve the Gameplay
- **Balanced Item Distribution**  
- **Item Movement Animation:**  
  Implemented smooth animations with **DOTween**, using **callback pattern** to ensure checks occur after animation completes  
- **Time Attack Mode:**  
  - Added flag to check when in **TimeMode** → ignore bottom cells filling up  
  - Stored `originalCell` reference in each Item to track its starting position for returning  
  - Set **LevelTime** to `60s`  
