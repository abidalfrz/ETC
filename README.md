# Midterm Project - ETC Desktop

Nama: Muhammad Abid Baihaqi Al Faridzi  
NRP: 5025241133  
Prodi: Teknik Informatika  
Mata Kuliah: Pemrograman Berbasis Kerangka Kerja  
Kelas: D

File PRD: [PRD.pdf](https://github.com/abidalfrz/ETC/blob/main/PRD.pdf)

Video Demo: [YouTube](https://www.youtube.com/watch?v=fST3n_nGM44)

---

## Program ETC Desktop

ETC (*Elite TC Champions*) adalah aplikasi desktop untuk membantu mahasiswa
menemukan anggota tim, mencari rekrutmen lomba atau proyek, serta menemukan
mentor sesuai keahlian. Satu akun dapat membuat rekrutmen sekaligus melamar ke
rekrutmen mahasiswa lain.

Aplikasi dibangun menggunakan **C#**, **.NET 10**, dan **Avalonia UI** sehingga
dapat dijalankan pada Ubuntu maupun Windows. Data disimpan secara persisten di
**SQL Server** dan diakses menggunakan **ADO.NET** melalui package
`Microsoft.Data.SqlClient`.

## Fitur Utama

- Registrasi, login, logout, dan pengelolaan profil mahasiswa.
- Pengelolaan skill, pengalaman, foto, bio, dan tautan portofolio.
- Dashboard statistik pribadi.
- Pembuatan, pengeditan, publikasi, dan penutupan rekrutmen.
- Posisi, kuota, kategori lomba/proyek, jadwal, biaya, dan informasi kontak.
- Pencarian, filter, pagination, serta bookmark rekrutmen.
- Pengajuan, pembatalan, penerimaan, dan penolakan lamaran.
- Daftar lamaran milik pengguna dan daftar pelamar pada rekrutmen sendiri.
- Direktori mentor berdasarkan nama atau keahlian.
- Tautan Google Form untuk kebutuhan mentoring.
- Validasi input, pembatasan akses berdasarkan pemilik data, dan validasi kuota.
- Penyimpanan data menggunakan SQL Server dengan schema dan data demo.
- Tema antarmuka desktop menggunakan Avalonia dan Fluent theme.

## Teknologi dan Arsitektur

| Komponen | Teknologi |
| --- | --- |
| Bahasa dan runtime | C#, .NET 10 |
| Antarmuka desktop | Avalonia UI |
| Pola aplikasi | MVVM dan data binding |
| Command | CommunityToolkit.Mvvm |
| Database | Microsoft SQL Server |
| Akses data | ADO.NET dan `Microsoft.Data.SqlClient` |
| Infrastruktur lokal | Docker |
| Pengujian | .NET Unit Test dan Integration Test |

Aplikasi memisahkan tampilan, state, aturan bisnis, dan akses database. ViewModel
menangani state serta command, service menangani proses bisnis, sedangkan
infrastructure menangani koneksi dan inisialisasi SQL Server.

## Struktur Project

```text
ETC/
├── database/
│   ├── 001-schema.sql
│   ├── 002-catalog.sql
│   └── 003-mentor-directory.sql
├── src/Etc.Desktop/
│   ├── Features/              # Accounts, dashboard, rekrutmen, lamaran, mentor
│   ├── Infrastructure/        # Koneksi dan inisialisasi database
│   ├── Shared/                # Komponen dan state bersama
│   ├── Styles/                # Token dan style Avalonia
│   ├── Assets/                # Foto mentor, ikon, font, dan data pendukung
│   ├── App.axaml
│   ├── MainWindow.axaml
│   └── Program.cs
├── tests/
│   ├── Etc.UnitTests/
│   └── Etc.IntegrationTests/
├── tools/Etc.RenderChecks/
├── Etc.slnx
├── PRD.pdf
├── README.md
├── global.json
└── Directory.Build.props
```

Database utama terdiri dari tabel mahasiswa, skill, rekrutmen, posisi, lamaran,
bookmark, dan mentor. File SQL pada folder `database/` ditanam ke dalam aplikasi
saat proses build.

## Cara Menjalankan

### Prasyarat

- SDK .NET 10 sesuai konfigurasi `global.json`.
- Docker Engine pada Ubuntu atau Docker Desktop pada Windows.
- Komputer dengan sesi desktop untuk menampilkan window Avalonia.

Periksa instalasi:

```bash
dotnet --version
docker version
```

### 1. Jalankan SQL Server

Contoh berikut menggunakan password sementara. Ganti password tersebut jika
diperlukan dan gunakan password yang sama pada connection string.

```bash
docker run --name etc-sql \
  --env ACCEPT_EULA=Y \
  --env MSSQL_PID=Developer \
  --env MSSQL_SA_PASSWORD='Etc-Sql!2026Aa' \
  --publish 127.0.0.1:1433:1433 \
  --volume etc-sql-data:/var/opt/mssql \
  --detach mcr.microsoft.com/mssql/server:2022-latest
```

Buat database `ETC_Demo`:

```bash
docker exec -it etc-sql /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -C -b \
  -Q "IF DB_ID(N'ETC_Demo') IS NULL CREATE DATABASE ETC_Demo;"
```

### 2. Atur koneksi dan siapkan aplikasi

Pada Ubuntu atau Bash:

```bash
export ETC_CONNECTION_STRING='Server=127.0.0.1,1433;Database=ETC_Demo;User ID=sa;Password=Etc-Sql!2026Aa;Encrypt=true;TrustServerCertificate=true'

dotnet restore
dotnet build
dotnet run --project src/Etc.Desktop -- --init-db
dotnet run --project src/Etc.Desktop -- --seed-demo
```

Pada PowerShell, gunakan connection string berikut sebelum menjalankan perintah
`dotnet`:

```powershell
$env:ETC_CONNECTION_STRING = 'Server=127.0.0.1,1433;Database=ETC_Demo;User ID=sa;Password=Etc-Sql!2026Aa;Encrypt=true;TrustServerCertificate=true'
```

### 3. Buka aplikasi

```bash
dotnet run --project src/Etc.Desktop
```

Seeder menyediakan akun demonstrasi. Password default akun demo adalah
`Etc-Demo!2026`, kecuali diubah melalui environment variable
`ETC_DEMO_PASSWORD` saat proses seed.

Untuk menjalankan kembali setelah setup selesai:

```bash
docker start etc-sql
dotnet run --project src/Etc.Desktop
```

Jika terminal baru digunakan, atur kembali `ETC_CONNECTION_STRING` karena
environment variable hanya berlaku pada terminal yang sedang aktif.

## Pengujian

Unit test dapat dijalankan dengan:

```bash
dotnet test tests/Etc.UnitTests
```

Integration test membutuhkan `ETC_TEST_CONNECTION_STRING` yang mengarah ke
database pengujian terpisah, bukan `ETC_Demo`:

```bash
dotnet test Etc.slnx
```

## Catatan Batasan

ETC merupakan aplikasi demonstrasi desktop. Aplikasi tidak mencakup chat,
notifikasi real-time, pembayaran, panel admin, deployment publik, atau
sinkronisasi offline. Data tersimpan pada SQL Server lokal dan sesi login
berakhir ketika aplikasi ditutup.
