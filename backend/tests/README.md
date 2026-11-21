# Backend tests

Project `SportsCourtBooking.Tests/` dùng xUnit để kiểm thử tự động backend, bao gồm kiểm thử đơn vị cho quy tắc miền/use case và test tích hợp PostgreSQL.

Chạy suite thường:

```powershell
dotnet test backend/SportsCourtBooking.sln
```

Bật test PostgreSQL:

```powershell
$env:RUN_POSTGRES_INTEGRATION_TESTS="1"
$env:SPORTS_COURT_TEST_CONNECTION="Host=localhost;Port=5432;Database=sportscourt;Username=postgres;Password=12345"
dotnet test backend/SportsCourtBooking.sln
```

Test tích hợp kiểm tra chỉ một booking thành công khi hai khách đặt đồng thời cùng slot và kiểm tra booking quá hạn được chuyển sang `Expired`, nhả slot. Không đặt biến tích hợp thì hai test này tự bỏ qua để suite unit có thể chạy độc lập.
