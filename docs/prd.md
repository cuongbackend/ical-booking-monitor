# Product Requirements Document — iCal Booking Monitor

## Executive Summary

iCal Booking Monitor là ứng dụng nền (.NET 8 Worker Service) chạy 24/7 như Windows Service trên máy tính cá nhân. Ứng dụng định kỳ quét các link iCal từ các nền tảng cho thuê phòng (AirBnb, Dayladau…), phát hiện booking mới và gửi thông báo ngay lập tức qua Telegram. Mục tiêu giúp chủ nhà phản hồi khách nhanh hơn mà không cần mở liên tục các ứng dụng nền tảng.

---

## Goals

- Quét tự động link iCal theo chu kỳ 10 phút.
- Phát hiện booking mới (chưa từng thấy) và gửi thông báo Telegram ngay.
- Hỗ trợ nhiều phòng, mỗi phòng có nhiều link iCal từ nhiều nền tảng.
- Chạy ổn định 24/7 như Windows Service, tự khởi động lại sau khi máy reboot.
- Cấu hình dễ dàng qua file `appsettings.json` mà không cần biên dịch lại.

## Non-goals

- Không hỗ trợ cancel/modify booking (chỉ phát hiện booking mới).
- Không cung cấp giao diện web hoặc dashboard.
- Không tích hợp trực tiếp API AirBnb / Dayladau (chỉ dùng iCal export link).
- Không hỗ trợ multi-tenant hay nhiều người dùng.
- Không lưu trữ lịch sử booking lâu dài (chỉ cần đủ để so sánh lần quét trước).

---

## User Stories

| ID | As a... | I want... | So that... |
|----|---------|-----------|------------|
| US-01 | Chủ nhà | Nhận thông báo Telegram ngay khi có booking mới | Tôi có thể phản hồi khách nhanh nhất có thể |
| US-02 | Chủ nhà | Theo dõi nhiều phòng cùng lúc | Tôi không cần check từng nền tảng riêng lẻ |
| US-03 | Chủ nhà | Cấu hình link iCal và Telegram token qua file config | Tôi có thể cập nhật cấu hình mà không cần kỹ năng lập trình |
| US-04 | Chủ nhà | Ứng dụng tự chạy lại sau khi máy khởi động | Không bị gián đoạn do mất điện hoặc reboot |
| US-05 | Chủ nhà | Không nhận thông báo trùng lặp cho cùng một booking | Tránh spam và nhầm lẫn |
| US-06 | Chủ nhà | Xem log khi có lỗi kết nối iCal | Biết được phòng nào đang bị sự cố |
| US-07 | Chủ nhà | Thêm phòng mới mà không nhận ảo notification | Lần đầu sync chỉ để lưu trạng thái, không gửi thông báo |

---

## Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-01 | Hệ thống quét tất cả link iCal đã cấu hình mỗi 10 phút | Must Have |
| FR-02 | Parse file iCal và trích xuất các sự kiện VEVENT có STATUS không phải CANCELLED | Must Have |
| FR-03 | So sánh UID của booking mới với danh sách đã lưu | Must Have |
| FR-04 | Gửi Telegram message khi phát hiện UID chưa từng thấy | Must Have |
| FR-05 | Lưu trạng thái đã biết (known UIDs) vào file JSON sau mỗi lần quét | Must Have |
| FR-06 | Hỗ trợ cấu hình nhiều phòng, mỗi phòng có tên và nhiều iCal URL | Must Have |
| FR-07 | Telegram message bao gồm: tên phòng, ngày check-in, ngày check-out, tên khách (nếu có) | Must Have |
| FR-08 | Khi iCal URL không truy cập được, log lỗi và tiếp tục với các URL khác, không crash | Must Have |
| FR-09 | Lần đầu chạy (hoặc thêm phòng mới), lưu state hiện tại mà không gửi notification | Must Have |
| FR-10 | Bỏ qua VEVENT có STATUS = CANCELLED | Should Have |
| FR-11 | Cấu hình interval quét có thể thay đổi qua appsettings.json (mặc định 10 phút) | Should Have |
| FR-12 | Chạy như Windows Service với sc.exe hoặc `dotnet publish` + Windows Service installer | Must Have |

---

## Non-functional Requirements

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-01 | Độ trễ thông báo | Tối đa 10 phút từ khi booking xuất hiện trên iCal |
| NFR-02 | Độ ổn định | Uptime ≥ 99% trong điều kiện máy tính cá nhân bình thường |
| NFR-03 | Tài nguyên | CPU < 1%, RAM < 50 MB khi idle |
| NFR-04 | Khả năng cấu hình | Thêm/sửa phòng chỉ cần sửa appsettings.json, restart service |
| NFR-05 | Log | Ghi log ra file với mức INFO/WARNING/ERROR, xoay vòng theo ngày |
| NFR-06 | Bảo mật | Telegram token và Bot ID lưu trong appsettings.json, không commit lên repo |
| NFR-07 | Khả năng mở rộng | Hỗ trợ tối thiểu 20 phòng / 50 link iCal mà không giảm hiệu năng |
| NFR-08 | Khởi động lại | Windows Service tự khởi động lại sau khi máy reboot |
