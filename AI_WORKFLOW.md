# AI Code Agent Workflow - MMT Tools Project

## 📁 1. Quy tắc tệp tin & Kiến trúc (Strict File Architecture)

### ⚙️ Target Framework & Language Version
- **.NET Framework:** 4.8
- **C# Version:** 7.3 (mặc định)
- **Không dùng features từ C# 8.0+** (switch expressions, recursive patterns, nullable reference types, etc.)

Dự án sử dụng **Partial Classes** để chia nhỏ code. Tuyệt đối tuân thủ phân vùng sau:

### ❌ CẤM (Forbidden)
| File | Quy tắc |
|------|---------|
| `MainWindow.xaml.cs` | **KHÔNG BAO GIỜ** viết code vào file này |

### ✅ Vùng Hệ Thống (Core Logic)
| File | Chức năng |
|------|-----------|
| `MainWindow.SystemArguments.cs` | Khai báo **links (B)**, **arguments (C)**, hằng số, data model, cấu hình |
| `MainWindow.SystemButtons.cs` | Logic các nút điều khiển chung: **Select All, Select None, SelectNoneAllTabs, Install, Pause, Resume, Refresh Color, BtnDownloadPage** |
| `MainWindow.SystemBar.cs` | Xử lý **Progress Bar**, **Segment UI**, thông báo trạng thái |
| `MainWindow.RestoredMethods.cs` | Hàm bổ trợ, phương thức **dùng chung** cho toàn ứng dụng |

### ✅ Vùng Tính Năng (Tab Logic)
| File | Chức năng |
|------|-----------|
| `MainWindow.TabWinModMMT.cs` | Code UI và logic cho tab **WinModMMT** |
| `MainWindow.TabBrowser.cs` | Code UI và logic cho tab **Browser** |
| `MainWindow.TabGaming.cs` | Code UI và logic cho tab **Gaming** |
| `MainWindow.TabMultimedia.cs` | Code UI và logic cho tab **Multimedia** |
| `MainWindow.TabOffice.cs` | Code UI và logic cho tab **Office** |
| `MainWindow.TabPartition.cs` | Code UI và logic cho tab **Partition** |
| `MainWindow.TabPopular.cs` | Code UI và logic cho tab **Popular** |
| `MainWindow.TabRemoteDesktop.cs` | Code UI và logic cho tab **RemoteDesktop** |
| `MainWindow.TabSystem.cs` | Code UI và logic cho tab **System** |
| `MainWindow.TabWindows.cs` | Code UI và logic cho tab **Windows** |

**Quy tắc:**
- Code liên quan đến **Checkbox (A)** và logic riêng của Tab → nằm trong `MainWindow.Tab[TênTab].cs`
- Nếu tạo **Tab mới** → Tự động tạo file: `MainWindow.Tab[TênTabMới].cs`

---

## 🔄 2. Quy trình làm việc (Workflow)

### Bước 1: AI Summary (Bắt buộc)
Khi **tạo mới** hoặc **cập nhật** bất kỳ file nào:
- Thêm khối comment ở **đầu file** tóm tắt chức năng
- Nếu update tính năng → Cập nhật summary với các thay đổi vừa thực hiện

**Ví dụ:**
```csharp
// =======================================================================
// MainWindow.TabPopular.cs
// Chức năng: Xử lý logic cho Tab Popular
// Cập nhật gần đây:
//   - 2026-03-03: Thêm InstallIDMAsync() với quy trình 7 bước
//   - 2026-03-03: Cập nhật argument IDM thành /s /a /u /o /quiet /skipdlgst
// =======================================================================
```

### Bước 2: Encoding
- Luôn lưu file ở định dạng **UTF-8** để hiển thị đúng tiếng Việt

### Bước 3: Cross-Interaction (Logic liên kết)
Khi tạo Checkbox (A) trong Tab X:
1. **Đăng ký** với `Select All`, `Select None`, `SelectNoneAllTabs` trong `MainWindow.SystemButtons.cs`
2. **Tiến trình chạy** phải báo cáo về `MainWindow.SystemBar.cs` (UpdateStatus, Progress Bar)
3. **Dữ liệu cấu hình** phải lấy từ `MainWindow.SystemArguments.cs`

### Bước 4: Verification
Sau khi code:
1. Thực hiện **Build** project
2. Nếu có **Error/Warning** → Tự động phân tích và **Fix Bug**
3. Lặp lại cho đến khi **Build thành công 100%**

### Bước 5: Checkpoint
Sau khi hoàn tất và Build thành công:
- Thông báo: **"✅ Checkpoint đã lưu"**
- Tóm tắt ngắn gọn những gì đã làm

---

## 🛠️ 3. Mẫu triển khai tính năng mới (Feature Template)

### Khi người dùng yêu cầu:
> "Trong tab X, tạo checkbox A, link B, argument C, tương tác được với các nút select all, select none, selectnonealltabs, install, pause, resume, refresh color, btndownloadpage"

### Step-by-Step Implementation:

#### **Step 1: Khai báo trong `MainWindow.SystemArguments.cs`**
```csharp
// Link B - Download URL
private const string IDM_DOWNLOAD_URL = "https://tinyurl.com/idmhcmvn";

// Argument C - Silent install arguments
private const string IDM_INSTALL_ARGUMENTS = "/s /a /u /o /quiet /skipdlgst";
```

#### **Step 2: Tạo UI Checkbox A trong `MainWindow.TabX.cs`**
```csharp
// Tạo Checkbox trong XAML hoặc code-behind
CheckBox ChkInstallIDM = new CheckBox
{
    Content = "Internet Download Manager",
    Name = "ChkInstallIDM"
};

// Tạo hàm InstallIDMAsync()
private async Task InstallIDMAsync()
{
    // Logic cài đặt với progress reporting
    UpdateStatus("Đang tải IDM...", "Cyan");
    await DownloadWithProgressAsync(IDM_DOWNLOAD_URL, idmPath, "IDM");
    
    // Sử dụng argument từ SystemArguments.cs
    ProcessStartInfo startInfo = new ProcessStartInfo
    {
        FileName = idmPath,
        Arguments = IDM_INSTALL_ARGUMENTS,
        UseShellExecute = true
    };
    // ...
}
```

#### **Step 3: Cập nhật `MainWindow.SystemButtons.cs`**
```csharp
// Đăng ký Checkbox vào danh sách điều khiển
private void UpdateInstallButtonState()
{
    bool hasChecked = ChkInstallIDM.IsChecked == true ||
                      ChkInstallWinRAR.IsChecked == true ||
                      // ... thêm checkbox mới
                      false;
    
    BtnInstall.IsEnabled = hasChecked;
}

// Xử lý nút Select All
private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
{
    ChkInstallIDM.IsChecked = true;
    ChkInstallWinRAR.IsChecked = true;
    // ...
    UpdateInstallButtonState();
}

// Xử lý nút Install
private async void BtnInstall_Click(object sender, RoutedEventArgs e)
{
    if (ChkInstallIDM.IsChecked == true)
        await InstallIDMAsync();
    
    if (ChkInstallWinRAR.IsChecked == true)
        await InstallWinRARAsync();
    
    // ...
}
```

#### **Step 4: Progress Reporting trong `MainWindow.SystemBar.cs`**
```csharp
// Phương thức UpdateStatus (đã có trong SystemBar.cs)
private void UpdateStatus(string message, string color)
{
    Dispatcher.Invoke(() =>
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    });
}

// Progress Bar updates
Dispatcher.Invoke(() =>
{
    DownloadProgressBar.Value = progress;
    ProgressTextBlock.Text = $"{progress}%";
});
```

---

## 📋 4. Checklist trước khi commit

- [ ] **AI Summary** đã được thêm/cập nhật ở đầu file
- [ ] File được lưu ở định dạng **UTF-8**
- [ ] **Không code** trong `MainWindow.xaml.cs`
- [ ] Links/Arguments đã khai báo trong `SystemArguments.cs`
- [ ] Checkbox đã đăng ký với `SystemButtons.cs` (Select All, Install, etc.)
- [ ] Progress reporting đã kết nối với `SystemBar.cs`
- [ ] **Build thành công 100%** (không Error/Warning)
- [ ] Đã thông báo **Checkpoint**

---

## 🎯 5. Ví dụ thực tế: IDM Installation (Tab Popular)

### User Request:
> "tab popular, checkbox IDM, khi click install: taskkill /im idman.exe, xóa IDMan.exe.bak, tải tinyurl.com/idmhcmvn, chạy /s /a /u /o /quiet /skipdlgst..."

### AI Implementation Flow:

1. **SystemArguments.cs**: Khai báo link và argument
2. **TabPopular.cs**: Tạo `ChkInstallIDM`, `InstallIDMAsync()` với 7 bước
3. **SystemButtons.cs**: Cập nhật `UpdateInstallButtonState()`, `BtnInstall_Click()`
4. **SystemBar.cs**: Sử dụng `UpdateStatus()` để báo cáo tiến trình
5. **Build & Verify**: Đảm bảo không lỗi
6. **Checkpoint**: Thông báo hoàn tất

---

## 📌 6. Ghi chú quan trọng

- **Partial Classes**: Tất cả file `MainWindow.*.cs` đều là partial class của `MainWindow`
- **Thread Safety**: Sử dụng `Dispatcher.Invoke()` khi cập nhật UI từ background thread
- **Async/Await**: Luôn dùng `async/await` cho các tác vụ I/O (download, file operations)
- **Error Handling**: Wrap code trong `try-catch` và báo cáo lỗi qua `UpdateStatus()`
- **Cleanup**: Xóa file tạm sau khi sử dụng xong

---

**Generated for:** MMT Tools Project  
**Last Updated:** 2026-03-03  
**Purpose:** Ensure consistent AI agent behavior across all coding tasks
