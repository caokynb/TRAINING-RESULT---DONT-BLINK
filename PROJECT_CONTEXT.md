# DONT BLINK — Project Handover / Context

> Handover dành cho AI hoặc thành viên tiếp quản. Cập nhật **05-11-2026 (Asia/Saigon)** từ repo local, Google Sheet tracker và các chat project/repo còn truy cập được. Đây là bản tổng hợp các quyết định và trạng thái quan trọng, không phải transcript đầy đủ. Khi thông tin giữa các nguồn lệch nhau, tài liệu ghi rõ nguồn nào xác nhận điều gì.

## 1. Bắt đầu nhanh: trạng thái và việc nên làm tiếp

- Project là game 3D Unity **DONT BLINK / BACKROOMS: DON'T BLINK**, sản phẩm cho đợt tuyển/training Gen mới của Game Creator Club (GCC) 2026.
- Repo hiện mở tại nhánh `develop`, HEAD `77dfe03`, đang trùng `origin/develop` ở thời điểm snapshot.
- Tracker Google Sheet ghi **30/62 việc hoàn tất (48%)**, tương đương 53/108 giờ. M0 và M1 xong; M2, tích hợp/QA tiếp theo chưa xong trên tracker.
- Repo đã có phần lớn khung core loop: player movement/look, manual/forced blink, quái tiến khi mắt đóng, scene bootstrap, tutorial, win/lose UI và logic exit. Có mã nguồn không đồng nghĩa đã xác nhận chơi được end-to-end.
- Việc tiếp theo hợp lý nhất: chạy Unity từ `Main` cùng người dùng, kiểm tra luồng tutorial → di chuyển/blink → quái → Exit → thắng/thua → Retry; đối chiếu từng lỗi với tracker (đặc biệt A09, K11, M2/K13). Sau đó cập nhật tracker theo bằng chứng thực tế.
- Không có xác nhận Play Mode hoặc build Windows thành công. Không khẳng định đã QA.
- Working tree có thay đổi cục bộ chưa commit ở `.vscode/settings.json`, TMP fallback asset và `ProjectSettings/ProjectAuditorSettings.asset`, cùng chính file context này. **Giữ nguyên, không reset/revert/stash hay commit các thay đổi đó nếu chưa có chỉ dẫn rõ.**

## 2. Project identity, mục đích và ý tưởng

- **Repo:** `https://github.com/caokynb/TRAINING-RESULT---DONT-BLINK`
- **Engine:** Unity `6000.5.8f1` (tên hiển thị Unity 6.5.8f1), URP. ProjectVersion trong repo xác nhận `6000.5.8f1`; tiêu đề sheet cũng ghi Unity 6.5.8f1.
- **Nhóm:** 3 người — Lộc, Kỳ (user), Thành. Vai trò theo tracker: Lộc phụ trách Art/map/visual; Kỳ phụ trách shared contract, Player/Blink/UI/Core/integration; Thành phụ trách MapTools/monster/audio. Phân công có thể đổi; tracker là nguồn hiện hành cho nhiệm vụ.
- **Ý tưởng user mô tả trong chat:** game 3D lấy cảm hứng từ Level 1 Backrooms. Người chơi bị quái đuổi, quái chỉ tiến khi người chơi nhắm mắt/blink. Thanh mỏi mắt đầy sẽ buộc nhắm mắt; mục tiêu ban đầu là đi tìm chìa khóa, mở cửa, nhập mã sinh viên để xem kết quả training đỗ/trượt. User mô tả game cần gọn, chơi liền, core chính là cơ chế này.
- **Độ chắc chắn về scope:** user nói sẽ gửi GDD chi tiết sau, nhưng trong các chat/repo đã rà soát không thấy GDD đầy đủ được bàn giao. Vì vậy tracker hiện là nguồn đặc tả vận hành tốt nhất. Chưa thấy hệ thống chìa khóa/inventory hoặc nhập mã sinh viên/kết quả training trong code hiện tại; cần xác nhận scope trước khi phát triển thêm.
- **Ngôn ngữ cộng tác:** tiếng Việt.

## 3. Core loop và các quyết định thiết kế đã chốt

1. Người chơi đi trong map Backrooms bằng WASD, nhìn bằng chuột.
2. Giữ **Space** để nhắm mắt chủ động. Quyết định đã chốt trong chat: khi blink chủ động, player vẫn được đi; fatigue tiếp tục hồi khi đang nhắm và thả Space sẽ mở mắt.
3. Fatigue tăng dần; khi đầy, forced blink xảy ra khoảng 1 giây và **khóa di chuyển**. Theo code hiện tại, tốc độ mỏi mặc định đầy trong khoảng 12 giây.
4. Quái tiến theo lưới/BFS khi mắt nhắm, dừng khi mở mắt; có grace period đầu game 10 giây; bắt player dưới 1.8m.
5. Player tới Exit để thắng; quái bắt được thì thua. End screen có nút chơi lại về `Main`.
6. Chìa khóa và student ID/result là ý tưởng ban đầu, chưa xác nhận triển khai. Cơ chế Exit trong code hiện tại dường như là nhìn/aim cửa để mở trigger rồi đi qua, không có check chìa khóa.

Các giá trị trong code có thể thay đổi qua Inspector. Không xem các con số mặc định là cân bằng cuối cùng; tracker dự kiến K14/K16/T14/T15 cho settings và playtest.

## 4. Nguồn trạng thái và độ tin cậy

### Repo tại snapshot

- `git status --short --branch`: `## develop...origin/develop`; modified `.vscode/settings.json`, `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`, `ProjectSettings/ProjectAuditorSettings.asset`; `PROJECT_CONTEXT.md` mới/modified để tạo handover.
- HEAD: `77dfe03 Merge branch 'ky/d3-endscreen' into develop`.
- Commit trước đó gần nhất: `e5819ac End Screen w/ Win & Lose`; trước đó có `c35e981 Raycast người chơi chạm cửa -> bật trigger exit`, `e3b669c Game Manager`, `de95d7e Blinking & moving fix`.
- `develop` đang đồng bộ `origin/develop`; không có thay đổi code gameplay chưa commit tại snapshot, ngoài file handover tạo trong lượt này.
- Remote `origin`: repo GitHub ở trên.
- EditorBuildSettings bật `Main`, `Level01_Art`, `Level01_Monster`, `Level01_Systems`; `SampleScene` bị tắt.
- Các branch local/remote cũ còn trong clone: `ky/d1-*`, `ky/d2-*`, `ky/d3-*`, `thanh/d2-builder` và remote `art/*`, `thanh/*`. Nhánh phát triển hiện tại vẫn là `develop`; không suy ra các branch cũ còn đang được dùng.

### Tracker Google Sheet

- Tên: **DON'T BLINK - CHECK LIST**.
- URL: [Project tracker](https://docs.google.com/spreadsheets/d/1iHmSqqTAD2Ba3R4CpOD6bvn3MlRV7InJZtSZ7Tz_Mfw/edit?gid=1280875476#gid=1280875476), tab/gid `1280875476`.
- Sheet cho biết trạng thái khi đọc ngày 05-11-2026; trạng thái bảng có thể thay đổi sau đó.
- Tổng quan trong sheet: Lộc 9/15 việc, 18/32 giờ (60%); Kỳ 10/20 việc, 17/34 giờ (50%); Thành 9/18 việc, 18/36 giờ (50%); tất cả gồm milestone: 30/62 việc (48%), 53/108 giờ.
- Sheet quy ước trạng thái: `Chưa làm`, `Đang làm`, `Chờ review`, `Xong`, `Bị chặn`. Cột Người chỉ dùng `Lộc / Kỳ / Thành / Cả nhóm`.
- Nhịp làm việc ghi trong sheet: họp 10:00 trong 10 phút để cập nhật bảng; cửa sổ merge `develop` lúc 17:30; sau 18:00 không merge. README_TEAM.md được sheet nhắc tới để chứa luật Git, ownership file và code contract nhưng file đó **không có trong repo snapshot**.
- Mốc M4 trong sheet ghi feature freeze lúc 12:00 ngày 5; buổi chiều chỉ playtest/sửa bug. M4 vẫn `Chưa làm` trên snapshot tracker ngày 05-11. Không rõ ngày “ngày 5” là ngày thứ năm của lịch 6 ngày hay ngày lịch; không tự suy diễn deadline.

### Lịch sử chat project/repo — các mốc chính

- Ban đầu user báo nhóm 3 người, đưa tracker, mô tả ý tưởng game và nói sẽ gửi GDD sau. Chat sau đó được dùng để giải quyết từng task K và hỗ trợ thao tác Unity.
- **K07:** lúc đầu thống nhất chờ scene Art của Lộc (A05/A06) và Monster của Thành (T05), tránh tự tạo trùng. Sau đó user tự tạo/lưu các scene `Main`, `Level01_Systems` và cấu hình Build Profile; AI viết `SceneBootstrap` để nạp additive và spawn Player tại PlayerSpawn. Tracker đánh K07 `Xong`; repo có đủ scene và build settings bật.
- **K05/K06:** BlinkSystem và HUD được triển khai; user chốt manual Space cho phép đi lại, forced blink khóa movement. Đây là quyết định cần giữ.
- **K08:** GameManager dùng state `Playing/Won/Lost`, ExitDetector phát sự kiện Exit, caught khóa controls. User đã thao tác dựng các phần Unity cần thiết; tracker K08 `Xong`.
- **K09:** user chọn tự tạo UI hierarchy trong Unity để AI code/wire; chọn nút Retry thay vì phím R. Hierarchy gồm EventSystem (InputSystemUIInputModule), EndScreenCanvas, EndPanel, ResultText, RetryButton/Label. User xác nhận đã setup xong UI. EndScreenView được nối serialized refs. Text thắng “Thoát được rồi!”, thua “Mày chậm quá!”, Retry tải lại Main. Sau đó user yêu cầu Blink HUD nằm dưới panel thắng/thua; code đổi HUD Canvas sortingOrder từ 100 thành -1, EndScreenCanvas ở 0.
- Theo các trao đổi khi làm K07–K09, đã xem `git diff --check`, nhưng scene YAML có trailing whitespace từ các thao tác Unity ở thời điểm đó; không có xác nhận Play Mode end-to-end.
- Một chat GitHub/SourceGit liên quan: user nói popup “Choose an account” xuất hiện khi thao tác với repo, xác nhận dùng **SourceGit**, rồi chọn trường hợp popup khi Push/Pull/Fetch. Hướng dẫn trước đó gợi ý kiểm tra account SourceGit/Git Credential Manager; chưa có kết luận đã giải quyết. Nếu user nhắc lại, hỏi ảnh/tên popup trước khi sửa credential toàn máy.

## 5. Google Sheet: trạng thái chi tiết

### Đã `Xong` trên sheet (28 task + 2 milestone)

- Art/Lộc: `A01`, `A02`, `A03`, `A04`, `A05`, `A06`, `A07`, `A08`, `A10`.
- Kỳ: `K01`–`K10`.
- Thành: `T01`–`T09`.
- Milestone nhóm: `M0`, `M1`.

### Chưa xong/đang làm trên sheet (đây là tracker status, chưa phải QA status)

- **Art/Lộc:** `A09` Chưa làm; `A11` Đang làm; `A12`, `A13`, `A14`, `A15`, `A16`, `A17` Chưa làm.
- **Kỳ:** `K11`, `K12`, `K13`, `K14`, `K15`, `K16`, `K17`, `K18`, `K19`, `K20`, `K21` Chưa làm.
- **Thành:** `T10`, `T11`, `T12`, `T13`, `T14`, `T15`, `T16`, `T17`, `T18` Chưa làm.
- **Milestone:** `M2`, `M3`, `M4`, `M5` Chưa làm.

### Ý nghĩa và thứ tự phụ thuộc quan trọng

- `A09`: cấu hình Exit ở scene Art, BoxCollider trigger/tag đúng để phát hiện. Tracker chưa tick, dù repo đã có `ExitDoorTrigger`, `ExitDetector` và event Exit; cần xác nhận scene/runtime behavior.
- `K11`: tích hợp/fix collision Player; phụ thuộc `A09` và `T08`. Chấp nhận khi đi sát tường không kẹt/lọt sàn.
- `M2`: milestone gameplay: Space làm quái tới gần; khi chạm thua; chạm Exit thắng. Phụ thuộc `T08`, `K08`; hiện chưa xong trên sheet.
- `A11`: model/visual monster pose prefab, đang làm; sau A10. Cần tên child `Pose_01`... chính xác.
- `K12`: menu đầu game (Bắt đầu/Thoát), pause/resume bằng Esc; giao Lộc theo tracker, phụ thuộc K09. (Hiện `PlayerInputReader` phát PausePressed nhưng chưa có pause controller được xác nhận.)
- `K13`: tích hợp chạy thử toàn game, spawn/camera/collision; phụ thuộc M2. Tiêu chí: chơi từ đầu đến win/lose không lỗi chặn.
- `K14`: GameSettings/F1 debug sau K13; tracker nói Thành dùng MonsterSettings riêng.
- `T10`: PoseSwitcher, phụ thuộc A11,T07. `T11`: ambient + blink audio, phụ thuộc K02; `T12`: tiếng bước chân khi mắt nhắm, phụ thuộc T11/T07.
- `M3`: milestone chơi được từ đầu đến cuối, phụ thuộc K13.
- Sau M3: A12–A17 art polish; K15 effects, K16 5 lượt playtest cùng Thành và cân bằng, K17 bugfix; T13 catch sequence, T14 difficulty settings, T15 playtest. `M4` feature freeze; rồi tối ưu/bugfix/build/release: A15–A17, K18–K21, T16–T18 và `M5`.
- M5 yêu cầu file build + tag `v0.1` trên `main`. Hiện repo ở `develop`, chưa có bằng chứng build/release.

## 6. Repo structure và code hiện tại

### Scenes / assets

- `Assets/01 SCENES/Main.unity`: scene entry chứa `SceneBootstrap`.
- `Assets/01 SCENES/Level01_Art.unity`: map, marker, và có MonsterSpawner trong scene.
- `Assets/01 SCENES/Level01_Monster.unity`: scene Monster/Spawner.
- `Assets/01 SCENES/Level01_Systems.unity`: GameManager và EndScreenCanvas/EndScreenView, EventSystem.
- `Assets/06 SANDBOX/`: `Loc.unity`, `Sandbox_Ky.unity`, `ThanhLVT.unity`.
- Player prefab: `Assets/04 PREFABS/Player/Player.prefab`; Monster prefab: `Assets/04 PREFABS/Monster/Monster.prefab`.
- Map file: `Assets/03 ASSETS/Maps/level01.txt`. Ký hiệu: `#` tường, `.` floor, `L` floor có đèn sáng, `P` player spawn, `M` monster spawn, `E` exit. File hiện không có ký hiệu key.
- MapBuilder docs: `Assets/02 SCRIPTS/MapTools/README_Map.md`; Unity menu **Tools > Backrooms > Build Map**, generated content nằm dưới `Generated`; build lại xóa root `Generated`; wall mix seed cố định 1337.
- README root hiện chỉ mô tả ngắn project. Không có `README_TEAM.md` hay `CREDITS.md` ở root theo snapshot.
- `Assets/05 SFX` hiện chỉ có thư mục/meta; không thấy asset audio trong inventory repo.

### Các script quan trọng

- `Assets/02 SCRIPTS/_SHARED/GameEvents.cs`: static events `OnPlayerCaught`, `OnCatchSequenceDone`, `OnPlayerReachedExit`.
- `Assets/02 SCRIPTS/_SHARED/BlinkEvents.cs`: static events `OnEyesClosed(float duration)`, `OnEyesOpened`, cùng trạng thái `EyesClosed`.
- `Assets/02 SCRIPTS/Player/Input/PlayerInputReader.cs`: tự tạo Input System actions. WASD/arrows, mouse delta, Space, Escape. Có `PausePressed` event nhưng không thấy logic pause listener hiện hành. Không thấy action R/F1 trong script này.
- `PlayerMovement.cs`: CharacterController, walkSpeed mặc định 4, gravity -20, khóa movement qua `SetMovementLocked`.
- `PlayerLook.cs`: yaw trên Player, pitch trên camera pivot; sensitivity mặc định 0.08.
- `Blink/BlinkSystem.cs`: fatigue mặc định 12s; manual blink cooldown 1.5s; manual hồi fatigue 1/s; forced duration 1s. Manual giữ Space cho phép movement; forced blink gọi SetMovementLocked(true), rồi mở mắt thì unlock. Gửi BlinkEvents.
- `UI/BlinkHudView.cs`: tạo HUD runtime (fatigue bar + overlay nhắm mắt), sortingOrder code hiện tại `-1`; lỗi font/fallback cần lưu ý do local TMP asset modified.
- `UI/TutorialScreenView.cs`: trên Player prefab; image `Assets/03 ASSETS/UI/Instructions.png` được gán (thay thế `Manual.png` cũ). Trong scene `Level01_Art`, `Tutorial Screen` có child `Background` dùng `Instructions.png` (bật) và `Instruction Image` cũ dùng `Manual.png` (tắt). Tạo tutorial Canvas runtime / liên kết canvas có sẵn, đóng băng gameplay, có nút `Xác nhận`, rồi khôi phục input/movement/look.
- `Core/SceneBootstrap.cs`: nạp additive Art/Systems/Monster; đặt Main active; tìm tag PlayerSpawn; spawn Player prefab; disable camera/AudioListener khác. Player prefab phải được gán trên Main; scene names phải có trong active Build Profile.
- `Monster/MonsterSpawner.cs`: spawn tại marker MonsterSpawn, dùng prefab hoặc fallback capsule; bổ sung MonsterBrain nếu thiếu; runtime auto-ensure spawner.
- `Monster/MonsterBrain.cs`: grace period 10s; khi nhắm mắt tiến theo BFS trên MapGrid (không NavMesh), nhiều bước tùy khoảng cách/thời gian giữ mắt nhắm; bắt dưới 1.8m và raise OnPlayerCaught. Có debug phím T.
- `Core/GameManager.cs`: state Playing/Won/Lost. Catch khóa input/look/blink; `waitForCatchSequence` hiện false trong scene nên lập tức kết thúc Lost bằng fallback. Exit thì Won.
- `Core/ExitDetector.cs`: trên Player, trigger/tag Exit phát event Exit.
- `MapTools/ExitDoorTrigger.cs`: raycast/look vào cửa để unlock exit trigger, đi vào trigger sẽ raise Exit event; có runtime auto-setup, không kiểm tra key inventory. Đây là cơ chế cửa hiện thấy trong code.
- `Core/EndScreenView.cs`: refs `EndPanel`, `ResultText`, `RetryButton` đã được serialize trong scene. Panel ban đầu ẩn; won/lost text; nút Retry load lại Main single. `EndScreenCanvas` sortingOrder 0. UI user tự dựng trong Unity; persistent Button OnClick list đang trống nhưng code listener được add/remove runtime.

### Dependencies / project settings

- `Packages/manifest.json`: Input System 1.20.0, URP 17.5.0, AI Navigation 2.0.14, uGUI 2.5.0, Timeline 1.8.12, Visual Scripting 1.9.12, Test Framework 1.7.0, và Unity modules.
- Tracker có nói Git LFS, Force Text, Visible Meta Files, tags/layers, `.gitignore` trong K01. Repo có Unity project config; cần kiểm tra `.gitattributes`/`.gitignore` nếu bắt đầu chỉnh Git, không suy ra cấu hình LFS thực tế từ tracker.

## 7. Working tree: các thay đổi chưa commit cần bảo toàn

`git status` ở snapshot:

```text
 M .vscode/settings.json
 M Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset
 M ProjectSettings/ProjectAuditorSettings.asset
?? PROJECT_CONTEXT.md
```

- `.vscode/settings.json`: tên solution đổi từ `Dont Blink - Training Result Game.slnx` thành `TRAINING RESULT - DONT BLINK.slnx`.
- `ProjectAuditorSettings.asset`: thêm default/platform groups cho Android, Standalone, WebGL.
- TMP fallback asset có diff lớn (atlas 512×512 xuống 1×1, character/glyph table rỗng trong diff được xem trước). Nguồn/ý định không rõ; kiểm tra trong Unity và xác nhận tiếng Việt/TMP trước khi commit hoặc phục hồi.
- File context này được tạo lại để handover; người dùng yêu cầu tạo tài liệu nên nội dung này là thay đổi có chủ đích.
- Đừng dùng `git reset`, `checkout --`, `restore`, `clean`, stash hoặc commit các file khác như một phần của việc bàn giao.

## 8. User workflow và cách tiếp quản hiệu quả

- User (Kỳ) thường muốn giữ workflow chia đôi: user làm thao tác Unity GUI nhanh hơn (tạo scene/object/hierarchy, chỉnh Inspector, kéo refs, xác nhận trực quan); AI phụ trách code và kiểm tra file/wiring, kèm checklist GUI cụ thể khi cần. User từng nói “như thường lệ thì giao việc cho tôi làm để bạn chỉ code thôi”.
- User đã tự setup end-screen hierarchy và xác nhận xong; đừng yêu cầu tạo lại UI. Khi có thêm setup editor cần, ghi chính xác scene/object/component/field và thứ tự thao tác.
- Trả lời tiếng Việt, trực tiếp, ít vòng; tiếp tục từ tiến trình thực tế. Tách rõ điều tracker báo, điều code cho thấy, điều đã test. Không nói “đã chạy ổn” nếu chưa quan sát Play Mode/build.
- Trước mọi sửa code, đọc `git status`, branch và diff liên quan để giữ thay đổi local. Tuân theo ownership/file-sharing tracker nếu sửa Scene/Art/Monster của thành viên khác; phối hợp với chủ file thay vì tự sửa sâu phần họ.
- User ưu tiên hướng dẫn có thể làm tiếp ngay, nhưng vẫn muốn quyết định GUI thuộc user và AI chỉ code khi đã chốt theo workflow trước.

## 9. Các unknowns/rủi ro xác minh trước khi thêm feature

1. Chạy Play Mode từ `Main`: xác nhận scenes load, marker spawn đúng, duplicate MonsterSpawner không tạo nhiều quái, tutorial có thể confirm, cursor/input hoạt động.
2. Kiểm tra manual/forced blink, thời gian grace 10s, BFS không xuyên tường, quái bắt đúng, player collision/đi sát tường. K11/M2 chưa hoàn tất trên sheet.
3. Kiểm tra luồng Exit thực tế. Sheet A09 chưa làm; code ExitDoorTrigger có raycast-open, ExitDetector có trigger event; repo có thể chưa đúng scene/flow hoặc tracker chưa cập nhật.
4. End screen và Retry: test cả Won/Lost và đảm bảo `Time.timeScale`/cursor/input được reset. Catch animation/sequence chưa có: GameManager scene bật fallback thua ngay (`waitForCatchSequence: 0`).
5. User idea có key + nhập student ID/result; không thấy trong code hiện tại. Hỏi user/GDD/tracker trước khi coi đây là yêu cầu hiện hành.
6. Pause/menu chưa làm theo tracker K12; hiện có PausePressed event từ Esc nhưng chưa thấy listener.
7. Audio, catch sequence, pose switching, difficulty curve, settings/debug overlay, playtest, Windows build/release đều chưa hoàn tất theo tracker hoặc không có bằng chứng trong repo.
8. Kiểm tra TMP Vietnamese glyph/fallback asset đã bị sửa local; không tự commit/revert.
9. Tracker và repo có điểm lệch: A09 chưa làm nhưng code exit tồn tại; T11/T12 chưa làm nhưng lịch sử có commit tên `thanh/d4-audio`/`c35e981` về raycast exit. Commit đó là xử lý cửa/exit, không phải bằng chứng có AudioManager/audio assets. Cần dùng code và playtest làm bằng chứng.

## 10. Prompt khởi động cho AI nhận bàn giao

Bạn đang hỗ trợ game Unity **DONT BLINK**. Hãy đọc `PROJECT_CONTEXT.md`, sau đó tự kiểm tra `git status`, branch và các file Unity liên quan trước khi sửa vì handover có thể cũ. Giữ nguyên thay đổi local chưa commit. Tracker hiện báo 30/62 tasks (48%); M2 và integrated QA chưa xong. Core code đã có Player/blink/monster/BFS/bootstrap/tutorial/end screen/exit logic, nhưng chưa được xác nhận Play Mode end-to-end; key + student ID/result chưa thấy implement. User Kỳ thường xử lý thao tác Unity GUI, AI viết code và đưa checklist cụ thể bằng tiếng Việt. Pick up bằng việc phối hợp user test từ Main và xử lý các lỗi/tasks K11, A09, M2/K13 theo đúng ownership. Không đánh dấu task hoàn tất hoặc khẳng định build/test thành công khi chưa kiểm chứng.
