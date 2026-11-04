# Hướng dẫn sử dụng MapBuilder (Backrooms)

## 1. Cấu trúc ký tự trong file `level01.txt`
- `#`: Ô tường (không đi qua được).
- `.`: Ô sàn đi được (sinh `Floor_Carpet` + `Ceiling_Tile` + ngẫu nhiên `Ceiling_Light_Turn_Off`).
- `L`: Ô sàn có đèn sáng (sinh `Floor_Carpet` + `Ceiling_Tile` + `Ceiling_Light`).
- `P`: Vị trí xuất phát của người chơi (sinh marker `PlayerSpawn` với Tag `PlayerSpawn`).
- `M`: Vị trí xuất phát của quái (sinh marker `MonsterSpawn` với Tag `MonsterSpawn`).
- `E`: Vị trí lối thoát (sinh marker `Exit` với Tag `Exit` và gắn `DoorFrame_Exit` ở cạnh tường giáp `#`).

## 2. Cách chạy Build Map
1. Trong Scene, tạo một GameObject gắn component `MapGrid`.
2. Kéo file text bản đồ (`Assets/03 ASSETS/Maps/level01.txt`) vào ô `Level File` và `BackroomsTheme.asset` vào ô `Map Theme`.
3. Trên thanh menu Unity, chọn **Tools > Backrooms > Build Map**.
4. Toàn bộ map sẽ được sinh tự động bên dưới root `Generated`:
   - Khi chạy lại **Build Map**, công cụ chỉ xóa `Generated` cũ và tạo lại, không ảnh hưởng tới object trang trí bên ngoài.
   - Tường `Wall_Clean` và `Wall_Dirty` được trộn ngẫu nhiên theo seed cố định (`1337`) để giữ nguyên kết quả qua mỗi lần build.
