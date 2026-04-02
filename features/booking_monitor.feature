Feature: Booking Monitor — iCal polling và Telegram notification
  Là chủ nhà sử dụng iCal Booking Monitor,
  Tôi muốn được thông báo ngay khi có booking mới từ AirBnb hoặc Dayladau,
  Để có thể phản hồi khách nhanh nhất có thể.

  Background:
    Given ứng dụng đã được cấu hình với Telegram Bot Token hợp lệ
    And ứng dụng đã được cấu hình với ChatId hợp lệ

  # ---------------------------------------------------------------------------
  # Scenario 1: Phát hiện booking mới → gửi Telegram
  # ---------------------------------------------------------------------------
  Scenario: Phát hiện booking mới và gửi thông báo Telegram
    Given phòng "Phòng 101" đã được cấu hình với iCal URL hợp lệ
    And state hiện tại của phòng "Phòng 101" không chứa UID "airbnb-uid-001"
    When worker chạy vòng quét mới
    And iCal URL trả về VEVENT với UID "airbnb-uid-001", check-in "2026-05-01", check-out "2026-05-03", summary "Nguyen Van A"
    Then hệ thống gửi Telegram message chứa "Phòng 101"
    And Telegram message chứa "01/05/2026"
    And Telegram message chứa "03/05/2026"
    And Telegram message chứa "Nguyen Van A"
    And UID "airbnb-uid-001" được lưu vào state của phòng "Phòng 101"

  # ---------------------------------------------------------------------------
  # Scenario 2: Không gửi thông báo trùng lặp cho UID đã biết
  # ---------------------------------------------------------------------------
  Scenario: Không gửi thông báo trùng lặp cho booking đã biết
    Given phòng "Phòng 101" đã được cấu hình với iCal URL hợp lệ
    And state hiện tại của phòng "Phòng 101" đã chứa UID "airbnb-uid-001"
    When worker chạy vòng quét mới
    And iCal URL trả về VEVENT với UID "airbnb-uid-001"
    Then hệ thống không gửi bất kỳ Telegram message nào

  # ---------------------------------------------------------------------------
  # Scenario 3: Nhiều booking mới cùng lúc
  # ---------------------------------------------------------------------------
  Scenario: Phát hiện nhiều booking mới trong một lần quét
    Given phòng "Phòng 101" đã được cấu hình với iCal URL hợp lệ
    And state hiện tại của phòng "Phòng 101" không chứa UID nào
    When worker chạy vòng quét mới
    And iCal URL trả về 3 VEVENT với UID "uid-001", "uid-002", "uid-003"
    Then hệ thống gửi đúng 3 Telegram message
    And cả 3 UID được lưu vào state của phòng "Phòng 101"

  # ---------------------------------------------------------------------------
  # Scenario 4: iCal URL không truy cập được → log lỗi, không crash
  # ---------------------------------------------------------------------------
  Scenario: iCal URL không truy cập được thì log lỗi và tiếp tục
    Given phòng "Phòng 101" được cấu hình với iCal URL không hợp lệ "https://invalid.example.com/cal.ics"
    And phòng "Phòng 102" được cấu hình với iCal URL hợp lệ
    And iCal URL của "Phòng 102" trả về VEVENT với UID "uid-102-new"
    And state không chứa UID "uid-102-new"
    When worker chạy vòng quét mới
    And request đến "https://invalid.example.com/cal.ics" trả về lỗi HTTP 404
    Then hệ thống ghi log lỗi cho iCal URL của "Phòng 101"
    And hệ thống không bị crash
    And hệ thống vẫn gửi Telegram message cho booking mới của "Phòng 102"

  # ---------------------------------------------------------------------------
  # Scenario 5: Booking bị cancelled → bỏ qua
  # ---------------------------------------------------------------------------
  Scenario: Bỏ qua booking có STATUS = CANCELLED
    Given phòng "Phòng 101" đã được cấu hình với iCal URL hợp lệ
    And state hiện tại của phòng "Phòng 101" không chứa UID "uid-cancelled"
    When worker chạy vòng quét mới
    And iCal URL trả về VEVENT với UID "uid-cancelled" và STATUS "CANCELLED"
    Then hệ thống không gửi bất kỳ Telegram message nào
    And UID "uid-cancelled" không được lưu vào state

  # ---------------------------------------------------------------------------
  # Scenario 6: Thêm phòng mới → initial sync không gửi notification
  # ---------------------------------------------------------------------------
  Scenario: Lần đầu sync phòng mới không gửi thông báo
    Given phòng "Phòng 103" vừa được thêm vào cấu hình
    And state chưa có dữ liệu cho phòng "Phòng 103"
    When worker chạy vòng quét đầu tiên cho phòng "Phòng 103"
    And iCal URL trả về 5 VEVENT với các UID khác nhau
    Then hệ thống không gửi bất kỳ Telegram message nào
    And tất cả 5 UID được lưu vào state của phòng "Phòng 103" như baseline
