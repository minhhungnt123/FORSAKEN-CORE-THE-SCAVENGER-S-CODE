# 🤖 Robot Assembly System - Hướng Dẫn Lắp Ráp

## 📖 Mục Lục

1. [Tổng Quan](#tổng-quan)
2. [Cài Đặt](#cài-đặt)
3. [Cách Sử Dụng](#cách-sử-dụng)
4. [Cấu Hình Socket](#cấu-hình-socket)
5. [Troubleshooting](#troubleshooting)
6. [API Reference](#api-reference)

---

## 🎯 Tổng Quan

Hệ thống lắp ráp robot cho phép người chơi **chọn và kết nối các bộ phận robot** trong một phòng chế tạo (Assembly Scene).

### Tính Năng Chính
- ✅ Giao diện chọn linh kiện (UI panel)
- ✅ Xem trước 3D real-time
- ✅ Kết nối vật lý tự động (FixedJoint)
- ✅ Kiểm tra hoàn thành lắp ráp
- ✅ Xoay camera để xem từ mọi góc độ
- ✅ Debug tools (F1/F2)

---

## 🔧 Cài Đặt

### Bước 1: Tạo Scene "RobotAssembly"

```
1. Unity → File → New Scene
2. Ctrl+S → Lưu tên: "RobotAssembly"
3. Lưu vào: Assets/Scenes/RobotAssembly.unity
```

### Bước 2: Auto Setup (Khuyến Khích)

```
Menu → Forsaken Core → Setup Assembly Scene
```

Script sẽ tự động tạo:
- ✅ Canvas UI
- ✅ Main Camera
- ✅ Directional Light
- ✅ AssemblyManager GameObject
- ✅ Tất cả components cần thiết

### Bước 3: Gán Module Prefabs

1. Tìm `AssemblyManager` trong Hierarchy
2. Inspector → `ModuleSelector` component
3. Gán các prefab:

```
Chassis Prefab → [ChassisModule prefab]
Head Prefab → [HeadModule prefab]
Arm Left Prefab → [ArmModule_Left prefab]
Arm Right Prefab → [ArmModule_Right prefab]
Leg Prefab → [LegModule prefab]
```

### Bước 4: Cấu Hình Socket

**Option A: Auto Reset (Nhanh nhất)**
```
Menu → Forsaken Core → Reset All Sockets to (0,0,0)
```

**Option B: Manual Setup**
- Mở ChassisModule Prefab
- Điều chỉnh vị trí từng socket:
  - HeadSocket: (0, 0, 0)
  - ArmLeftSocket: (0, 0, 0)
  - ArmRightSocket: (0, 0, 0)
  - LegSocket: (0, 0, 0)

---

## 🎮 Cách Sử Dụng

### Trong Game

#### 1. Chọn Linh Kiện

Khi play scene, bạn sẽ thấy giao diện với **5 nút bên trái**:

```
┌─────────────────┐
│  LINH KIỆN      │
├─────────────────┤
│ 🤖 THÂN ROBOT   │ ← Chọn trước!
│ 🧠 ĐẦU          │
│ 💪 TAY TRÁI     │
│ 💪 TAY PHẢI     │
│ 🦵 CHÂN         │
└─────────────────┘
```

**Quy tắc**: Phải chọn **THÂN** trước, sau đó chọn các bộ phận khác.

#### 2. Xem Robot

Robot sẽ hiển thị ở **giữa màn hình** (3D viewport).

- Mỗi khi chọn module mới, nó sẽ **spawn ra** ở bên cạnh
- Bạn có thể **kéo module** để xem từ góc khác

#### 3. Xoay Camera

```
Chuột phải + Di chuyển chuột
  → Camera xoay quanh robot
  → Xem được từ mọi góc độ
```

#### 4. Xác Nhận Lắp Ráp

Sau khi chọn tất cả bộ phận:

```
Nhấn nút: ✓ XÁC NHẬN
  ↓
Script sẽ:
  ✓ Kết nối từng module với socket
  ✓ Tạo FixedJoint (liên kết vật lý)
  ✓ Kiểm tra robot hoàn chỉnh
  ↓
Nếu thành công:
  ✓ Message: "Robot đã được lắp ráp hoàn chỉnh!"
  ✓ Nút XÁC NHẬN bị vô hiệu
```

#### 5. Các Nút Điều Khiển Khác

```
↻ RESET     → Xóa tất cả, bắt đầu lại
✕ THOÁT     → Quay lại menu
```

---

## 📐 Cấu Hình Socket

### Socket Là Gì?

Socket là một **Empty GameObject** trên Chassis dùng để:
- Định vị chính xác vị trí gắn module
- Liên kết vật lý (FixedJoint)
- Tính toán khối lượng

### Vị Trí Socket

Mỗi socket tương ứng với một module:

| Socket | Module | Vị Trí Mặc Định |
|--------|--------|-----------------|
| headSocket | Đầu | (0, 0, 0) |
| armLeftSocket | Tay Trái | (0, 0, 0) |
| armRightSocket | Tay Phải | (0, 0, 0) |
| legSocket | Chân | (0, 0, 0) |

### Cách Điều Chỉnh Socket Position

1. **Mở ChassisModule Prefab** trong Hierarchy
2. **Expand** → Tìm socket (ví dụ: HeadSocket)
3. **Select** socket
4. **Inspector** → Transform → Position
5. **Điều chỉnh** X, Y, Z sao cho khớp với module

**Lưu ý**: Khi module được connect:
```
module.transform.SetParent(socket);
module.transform.localPosition = Vector3.zero;
```

Module sẽ **đặt vào vị trí của socket** với localPosition = (0,0,0).

---

## 🛠️ Editor Tools

### Tool 1: Reset All Sockets

**Mục đích**: Set tất cả socket về (0, 0, 0)

```
Menu → Forsaken Core → Reset All Sockets to (0,0,0)
  ↓
Console log:
  ✓ HeadSocket → (0, 0, 0)
  ✓ ArmLeftSocket → (0, 0, 0)
  ✓ ArmRightSocket → (0, 0, 0)
  ✓ LegSocket → (0, 0, 0)
```

### Tool 2: Auto Sync Sockets

**Mục đích**: Tự động đồng bộ socket từ vị trí module

```
Menu → Forsaken Core → Setup Sockets - Auto Sync from Modules
  ↓
Script sẽ:
  1. Detect vị trí từng module con
  2. Set socket = module.localPosition
  3. In log kết quả
```

**Cách dùng**:
```
1. Đặt các module ở vị trí đúng trong scene
2. Chạy Auto Sync
3. Socket sẽ tự động update
4. Bấm XÁC NHẬN → Robot khớp chính xác!
```

### Tool 3: Debug Socket Positions

**Mục đích**: Xem vị trí hiện tại của tất cả socket

```
Menu → Forsaken Core → Debug - Show Socket Positions
  ↓
Console log tất cả socket position
```

---

## 🐛 Troubleshooting

### Vấn đề 1: Robot Bị Lệch Khi Kết Nối

**Nguyên nhân**: Socket position không khớp với module origin

**Giải pháp**:
```
1. Menu → Debug - Show Socket Positions
2. Check console xem socket ở đâu
3. Menu → Reset All Sockets to (0,0,0)
4. Hoặc manual điều chỉnh socket position
5. Test lại
```

### Vấn đề 2: Không Thể Chọn Module

**Nguyên nhân**: Chưa chọn THÂN trước

**Giải pháp**:
```
→ Phải chọn "🤖 THÂN ROBOT" trước!
→ Sau đó chọn các bộ phận khác
```

### Vấn đề 3: Module Không Kết Nối

**Nguyên nhân**: Socket reference chưa được gán

**Giải pháp**:
```
1. Check Inspector → RobotAssemblyManager
2. Verify Canvas được gán
3. Check ModuleSelector → Prefabs được gán
4. Check ChassisModule → Socket được gán
```

### Vấn đề 4: UI Không Hiển Thị

**Nguyên nhân**: Canvas chưa được tạo hoặc AssemblyUIBuilder chưa run

**Giải pháp**:
```
1. Check Canvas tồn tại trong Hierarchy
2. Play scene → UI sẽ được auto-create
3. Check Console log có error không
```

### Vấn đề 5: Camera Không Quay

**Nguyên nhân**: CameraTarget chưa được gán

**Giải pháp**:
```
1. Inspector → RobotAssemblyManager
2. Camera Target → Gán CameraTarget từ Hierarchy
3. Hoặc để trống → Script sẽ auto-create
```

---

## 📚 API Reference

### ModuleSelector

```csharp
// Chọn module theo tag
public void SelectModule(string moduleTag)
{
	// Tags: "CHASSIS", "HEAD", "ARM_LEFT", "ARM_RIGHT", "LEG"
	selector.SelectModule("HEAD");
}

// Xác nhận lắp ráp
public void ConfirmAssembly()
{
	selector.ConfirmAssembly();
}

// Reset selector
public void ResetSelector()
{
	selector.ResetSelector();
}

// Lấy Chassis hiện tại
public ChassisModule GetCurrentChassis()
{
	return selector.GetCurrentChassis();
}

// Lấy danh sách module đã chọn
public Dictionary<string, RobotModule> GetSelectedModules()
{
	return selector.GetSelectedModules();
}
```

### RobotAssemblyManager

```csharp
// Callback khi lắp ráp hoàn thành
public void OnAssemblyComplete(ChassisModule chassis)
{
	// Gọi tự động khi robot hoàn chỉnh
}

// Reset scene
public void ResetAssembly()
{
	assemblyManager.ResetAssembly();
}

// Thoát scene
public void ExitAssembly()
{
	assemblyManager.ExitAssembly();
}

// Check lắp ráp hoàn thành
public bool IsAssemblyComplete()
{
	return assemblyManager.IsAssemblyComplete();
}
```

### ChassisModule

```csharp
// Lắp module vào socket
public bool TryEquip(RobotModule module)
{
	return chassis.TryEquip(module);
}

// Tháo module
public void Detach(string socketTag)
{
	chassis.Detach(socketTag);
}

// Lấy module tại socket
public RobotModule GetEquipped(string socketTag)
{
	return chassis.GetEquipped(socketTag);
}

// Check robot hoàn chỉnh
public bool IsFullyAssembled()
{
	return chassis.IsFullyAssembled();
}

// Tính tổng khối lượng
public float GetTotalMass()
{
	return chassis.GetTotalMass();
}
```

---

## 💡 Ví Dụ Sử Dụng Code

### Lắp Ráp Từ Code

```csharp
ModuleSelector selector = FindObjectOfType<ModuleSelector>();

// Chọn các module
selector.SelectModule("CHASSIS");
selector.SelectModule("HEAD");
selector.SelectModule("ARM_LEFT");
selector.SelectModule("ARM_RIGHT");
selector.SelectModule("LEG");

// Xác nhận
selector.ConfirmAssembly();

// Check kết quả
ChassisModule robot = selector.GetCurrentChassis();
if (robot.IsFullyAssembled())
{
	Debug.Log("Robot lắp ráp thành công!");
	Debug.Log($"Tổng khối lượng: {robot.GetTotalMass()} kg");
}
```

### Kiểm Tra Trạng Thái Robot

```csharp
ChassisModule robot = selector.GetCurrentChassis();

// Check từng bộ phận
var head = robot.GetEquipped("HEAD_SOCKET");
var armL = robot.GetEquipped("ARM_L_SOCKET");
var armR = robot.GetEquipped("ARM_R_SOCKET");
var leg = robot.GetEquipped("LEG_SOCKET");

if (head == null) Debug.Log("Thiếu đầu!");
if (armL == null) Debug.Log("Thiếu tay trái!");
if (armR == null) Debug.Log("Thiếu tay phải!");
if (leg == null) Debug.Log("Thiếu chân!");
```

---

## 🎮 Debug Hotkeys

| Phím | Chức Năng |
|------|----------|
| F1 | Toggle debug panel |
| F2 | Print config to console |
| Delete | Xóa module được chọn |
| Chuột Phải + Drag | Xoay camera |

---

## 📋 Checklist Setup

- [ ] Tạo scene "RobotAssembly"
- [ ] Run auto setup script
- [ ] Gán module prefabs
- [ ] Cấu hình socket position
- [ ] Play scene → Test UI
- [ ] Chọn Chassis → OK?
- [ ] Chọn Head → OK?
- [ ] Chọn Arms → OK?
- [ ] Chọn Leg → OK?
- [ ] Bấm Confirm → Kết nối OK?
- [ ] Robot khớp đúng vị trí?

---

## 📞 Support

**Lỗi socket position?**
→ Xem [Vấn đề 1: Robot Bị Lệch](#vấn-đề-1-robot-bị-lệch-khi-kết-nối)

**Không thể chọn module?**
→ Xem [Vấn đề 2: Không Thể Chọn](#vấn-đề-2-không-thể-chọn-module)

**UI không hiển thị?**
→ Xem [Vấn đề 4: UI Không Hiển Thị](#vấn-đề-4-ui-không-hiển-thị)

---

## 📁 File Structure

```
Assets/_Scripts/
├─ UI/
│  ├─ ModuleSelector.cs
│  └─ AssemblyUIBuilder.cs
├─ Managers/
│  ├─ RobotAssemblyManager.cs
│  └─ AssemblyDebugger.cs
├─ Editors/
│  ├─ SocketSetupHelper.cs
│  ├─ SocketAutoSync.cs
│  └─ ResetAllSocketsToOrigin.cs
├─ Modules/
│  ├─ RobotModule.cs (base)
│  ├─ ChassisModule.cs
│  ├─ HeadModule.cs
│  ├─ ArmModule.cs
│  └─ LegModule.cs
```

---

**Tạo bởi**: Game Development Team  
**Game**: FORSAKEN CORE - THE SCAVENGER'S CODE  
**Ngôn ngữ**: C# + Unity  
**Phiên bản**: 1.0
