---
description: Thêm checkbox mới vào một tab trong MMT Tools và tự động cập nhật tất cả các nơi cần thiết
---

# Workflow: Thêm Checkbox Mới Vào Tab

Khi user yêu cầu "thêm checkbox [TÊN] vào tab [TAB]", thực hiện đúng theo các bước sau:

---

## Quy tắc phân loại file

| Loại code | Nơi đặt |
|---|---|
| `ChkXxx_Click` (event handler) | `MainWindow.Tab[TabName].cs` |
| `InstallXxxAsync()` (cài đặt) | `MainWindow.Tab[TabName].cs` |
| `BtnSelectAll_Click` → thêm `ChkXxx.IsChecked = true` | `MainWindow.SystemButtons.cs` |
| `BtnSelectNone_Click` → thêm `ChkXxx.IsChecked = false` | `MainWindow.SystemButtons.cs` |
| `BtnSelectNoneAllTabs_Click` → thêm `ChkXxx.IsChecked = false` | `MainWindow.SystemButtons.cs` |
| `BtnInstall_Click` → thêm dòng `if (ChkXxx...)` | `MainWindow.SystemButtons.cs` |
| `BtnStop_Click`, `BtnRefreshColor_Click`, `BtnDownloadPage_Click` | `MainWindow.SystemButtons.cs` |
| Checkbox có **argument** hoặc **button phụ** (VD: `/s`, `/silent`, `/passive`, `MessageBox.Show`, key dialog...) | `MainWindow.SystemArguments.cs` |
| Link tải file (download URL trong `BtnDownloadPage_Click`) | `MainWindow.SystemButtons.cs` |
| **Tất cả code liên quan đến progress bar, segment download** | `MainWindow.SystemBar.cs` |
| XAML: Thêm `<CheckBox x:Name="ChkXxx" ...>` vào tab tương ứng | `MainWindow.xaml` |

---

## Bước 1: Thêm vào XAML (`MainWindow.xaml`)

Thêm `<CheckBox>` vào đúng Tab section trong XAML:
- Tab Popular → tìm `<!-- Tab Popular -->` hoặc `Header="Popular"`
- Tab System → tìm `Header="System"`
- Tab Office → tìm `Header="Office"`
- Tab Gaming → tìm `Header="Gaming"`
- Tab Browser → tìm `Header="Browser"`
- Tab Multimedia → tìm `Header="Multimedia"`
- Tab Partition → tìm `Header="Partition"`
- Tab Remote Desktop → tìm `Header="Remote Desktop"`
- Tab Windows - Microsoft → tìm `Header="Windows - Microsoft"`

```xml
<CheckBox x:Name="ChkTenApp" Content="Tên Hiển Thị" Click="ChkTenApp_Click"/>
```

---

## Bước 2: Thêm Event Handler vào Tab CS (`MainWindow.Tab[TabName].cs`)

Thêm event handler `_Click` và hàm `InstallXxxAsync()`:

```csharp
private void ChkTenApp_Click(object sender, RoutedEventArgs e)
{
    if (ChkTenApp.IsChecked == true)
    {
        UpdateStatus("Đã chọn: Tên App", "Green");
    }
    else
    {
        UpdateStatus("Đã hủy chọn: Tên App", "Yellow");
    }

    UpdateInstallButtonState();
}


private async Task InstallTenAppAsync()
{
    try
    {
        UpdateStatus("Đang tải Tên App...", "Cyan");
        string tenAppPath = Path.Combine(GetGMTPCFolder(), "TenApp.exe");
        await DownloadWithProgressAsync("https://URL_TAI_VE", tenAppPath, "Tên App Installer");

        Dispatcher.Invoke(() =>
        {
            DownloadProgressBar.Value = 0;
            ProgressTextBlock.Text = "";
            SpeedTextBlock.Text = "";
        });

        UpdateStatus("Đang cài đặt Tên App...", "Yellow");
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = tenAppPath,
            Arguments = "/S",   // hoặc /silent, /passive, v.v.
            UseShellExecute = true
        };
        Process process = Process.Start(startInfo);
        if (process != null)
        {
            await Task.Run(() => process.WaitForExit());
            UpdateStatus("Cài đặt Tên App hoàn tất!", "Green");
        }

        if (File.Exists(tenAppPath))
            File.Delete(tenAppPath);
    }
    catch (Exception ex)
    {
        UpdateStatus($"Lỗi khi cài đặt Tên App: {ex.Message}", "Red");
    }
}
```

> **Lưu ý:** Nếu hàm cài đặt cần dùng argument phức tạp (MessageBox, key dialog, nhiều if-else cho argument), hãy chuyển hàm `InstallXxxAsync()` sang `MainWindow.SystemArguments.cs` thay vì để ở file tab.

---

## Bước 3: Thêm vào `BtnSelectAll_Click` (`MainWindow.SystemButtons.cs`)

Tìm block `if (tabHeader == "TenTab")` tương ứng và thêm:

```csharp
ChkTenApp.IsChecked = true;
```

---

## Bước 4: Thêm vào `BtnSelectNone_Click` (`MainWindow.SystemButtons.cs`)

Tìm block `else if (tabHeader == "TenTab")` tương ứng và thêm:

```csharp
ChkTenApp.IsChecked = false;
```

---

## Bước 5: Thêm vào `BtnSelectNoneAllTabs_Click` (`MainWindow.SystemButtons.cs`)

Thêm vào cuối phần comment tương ứng với tab:

```csharp
// Bỏ chọn TenApp
ChkTenApp.IsChecked = false;
```

---

## Bước 6: Thêm vào `BtnInstall_Click` (`MainWindow.SystemButtons.cs`)

Thêm vào danh sách tasks:

```csharp
if (ChkTenApp.IsChecked == true) tasks.Add((InstallTenAppAsync, ChkTenApp));
```

---

## Bước 7: Thêm vào `BtnDownloadPage_Click` (`MainWindow.SystemButtons.cs`)

Nếu checkbox có link tải file trực tiếp:

```csharp
if (ChkTenApp.IsChecked == true)
{
    downloadLinks.Add("https://URL_TAI_TRUC_TIEP");
}
```

> **Lưu ý:** Nếu checkbox KHÔNG có link tải trực tiếp (ví dụ: chỉ chạy script, hay tải từ web động), bỏ qua bước này.

---

## Bước 8: Thêm vào `BtnRefreshColor_Click` (`MainWindow.SystemButtons.cs`)

Thêm `ChkTenApp` vào mảng `allCheckBoxes`:

```csharp
ChkTenApp,
```

---

## Quy tắc cho `MainWindow.SystemArguments.cs`

File `MainWindow.SystemArguments.cs` chứa tất cả logic **phụ thuộc vào argument** hoặc **button phụ** của một checkbox, bao gồm:

- Hàm `InstallXxxAsync()` có nhiều nhánh argument (`/S`, `/passive`, v.v.) kết hợp với `MessageBox.Show()`
- Hàm `InstallXxxAsync()` cần hiện dialog key/serial sau khi cài
- Event handler `BtnXxx_Click` dành riêng cho một app cụ thể (VD: `BtnActivateNetLimiter_Click`, `BtnRunBIDActivation_Click`)
- `ShowXxxKeyDialog()` hoặc các dialog helper tương tự
- Logic kích hoạt (`Activate*`) hoặc crack

**Nếu InstallXxxAsync đơn giản (chỉ download → chạy với 1 argument duy nhất → xóa file), đặt ở Tab CS.**

> **QUAN TRỌNG ⚡ LUÔN CỐ ĐỊNH:** Quy tắc này áp dụng **vĩnh viễn**, kể cả khi bắt đầu chat mới. Nếu phát hiện code complex đang nằm sai file (Tab CS), phải move ngay sang `MainWindow.SystemArguments.cs`.

**Tiêu chí "phức tạp" (→ bắt buộc đặt ở SystemArguments.cs):**

| Dấu hiệu | Ví dụ |
|---|---|
| Có `MessageBox.Show()` | Hỏi Yes/No/Cancel trước khi cài |
| Có key/serial dialog | `ShowXxxKeyDialog()` |
| Nhiều nhánh argument | `if Yes → /passive`, `if No → bỏ argument` |
| Button phụ riêng cho app | `BtnActivateNetLimiter_Click`, `BtnRunBIDActivation_Click` |
| Logic activate/crack | `ActivateWindows()`, `RunIDMCrackAsync()` |
| Tải về Desktop và hiện MessageBox | `BtnBackupRestore_Click` |

**Nếu đơn giản (chỉ download → chạy 1 argument cố định → xóa file), đặt ở Tab CS.**

---

## Quy tắc cho `MainWindow.SystemBar.cs` ⚡ LUÔN CỐ ĐỊNH

> **QUAN TRỌNG:** Quy tắc này áp dụng **vĩnh viễn**, kể cả khi bắt đầu chat mới. Không bao giờ đặt code progress bar / segment vào bất kỳ file nào khác.

File `MainWindow.SystemBar.cs` chứa **toàn bộ** code liên quan đến:

- Progress bar: `DownloadProgressBar`, `ProgressTextBlock`, `SpeedTextBlock`, `ConnectionTraceGrid`
- Segmented download: logic chia segment, tính toán range, merge segment
- `DownloadWithProgressAsync()` và các overload của nó
- Cập nhật UI progress trong quá trình tải (`Dispatcher.Invoke` liên quan đến progress)
- Multi-connection / multi-segment tracking
- Bất kỳ hàm helper nào phục vụ cho việc hiển thị / tính toán tiến độ tải

---

## Mapping Tab Header → Tab CS File

| Tab Header (XAML) | CS File |
|---|---|
| `Popular` | `MainWindow.TabPopular.cs` |
| `System` | `MainWindow.TabSystem.cs` |
| `Office` | `MainWindow.TabOffice.cs` |
| `Gaming` | `MainWindow.TabGaming.cs` |
| `Browser` | `MainWindow.TabBrowser.cs` |
| `Multimedia` | `MainWindow.TabMultimedia.cs` |
| `Partition` | `MainWindow.TabPartition.cs` |
| `Remote Desktop` | `MainWindow.TabRemoteDesktop.cs` |
| `Windows - Microsoft` | `MainWindow.TabWindows.cs` |
