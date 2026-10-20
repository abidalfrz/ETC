# ETC Desktop

Aplikasi demonstrasi rekrutmen mahasiswa ke mahasiswa untuk lomba dan proyek. Satu akun dapat membuat rekrutmen dan melamar ke tim lain. Dibangun dengan C#/.NET 10, Avalonia, MVVM, ADO.NET, dan SQL Server.

Fitur: register/login, profil dan foto, rekrutmen dan posisi, pencarian, bookmark, lamaran dan seleksi pelamar, dashboard pribadi, serta direktori mentor dengan foto dan tautan Google Form. Aplikasi menggunakan tema terang.

## How to run

- [Setup pertama: langkah 1–10](#1-siapkan-folder-project)
- [Menjalankan setelah setup selesai](#menjalankan-setelah-setup-selesai)
- [Alternatif SQL Server yang sudah terpasang](#alternatif-sql-server-yang-sudah-terpasang)
- [Mengatasi error](#mengatasi-error)
- [Memperbarui data mentor](#memperbarui-data-mentor)
- [Struktur dan pengujian](#struktur-dan-pengujian)

## 1. Siapkan folder project

Salin atau ekstrak seluruh source ke komputer tujuan. Buka terminal di folder yang berisi `Etc.slnx`, bukan di folder `bin` atau `obj`.

Struktur yang perlu dipertahankan:

```text
ETC/
├── README.md
├── Etc.slnx
├── global.json
├── Directory.Build.props
├── database/                 # Semua SQL schema, katalog, dan migrasi
├── src/Etc.Desktop/          # Semua C#, AXAML, .csproj, packages.lock.json
│   └── Assets/              # Termasuk tabel mahasiswa, foto, font, ikon, lisensi
├── tests/                   # Project yang dirujuk oleh Etc.slnx
└── tools/Etc.RenderChecks/  # Opsional untuk pemeriksaan UI
```

## 2. Pasang kebutuhan komputer

Siapkan komputer **Ubuntu x64 dengan sesi desktop** atau **Windows x64**. Akses internet diperlukan untuk mengunduh SDK, image SQL, dan paket NuGet pertama kali.

1. Pasang **SDK .NET 10**, bukan hanya runtime, dari [halaman unduhan .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). Untuk instalasi per sistem operasi, lihat [panduan Microsoft](https://learn.microsoft.com/en-us/dotnet/core/install/). Project meminta SDK `10.0.112` dengan `rollForward: latestFeature`; gunakan SDK 10.0 yang memenuhi konfigurasi tersebut.
2. Ubuntu: pasang [Docker Engine](https://docs.docker.com/engine/install/ubuntu/) dan ikuti [pengaturan akses Docker tanpa sudo](https://docs.docker.com/engine/install/linux-postinstall/), kemudian masuk ulang jika diperlukan. Windows: pasang dan buka [Docker Desktop](https://docs.docker.com/desktop/setup/install/windows-install/), lalu gunakan **Linux containers**.
3. Sediakan minimal **2 GB RAM untuk SQL Server**, di luar kebutuhan sistem dan aplikasi. Image SQL Server pada panduan ini mendukung CPU Intel/AMD x86-64. Untuk komputer ARM, gunakan SQL Server pada mesin x64 lain melalui [konfigurasi alternatif](#alternatif-sql-server-yang-sudah-terpasang). Lihat [prasyarat resmi SQL Server Docker](https://learn.microsoft.com/en-us/sql/linux/install-upgrade/quickstart-install-docker?view=sql-server-linux-ver16).

Periksa dari terminal yang akan dipakai:

```bash
dotnet --version
docker version
```

`dotnet` harus menemukan SDK 10.0 yang sesuai. `docker version` harus menampilkan bagian **Client dan Server** tanpa error koneksi atau permission. Jangan lanjut jika salah satunya gagal.

Khusus Ubuntu, pasang pustaka desktop Avalonia:

```bash
sudo apt update
sudo apt install -y libx11-6 libice6 libsm6 libfontconfig1
```

Window aplikasi memerlukan sesi desktop X11/XWayland; terminal server tanpa display tidak cukup. Lihat [dukungan platform Avalonia](https://docs.avaloniaui.net/docs/supported-platforms). Pengujian project dilakukan pada Ubuntu; GUI native Windows tetap perlu dicoba di komputer tujuan.

## 3. Buat SQL Server lokal

Tahap ini dilakukan **sekali untuk setup baru**. Periksa dahulu apakah ada percobaan sebelumnya, memakai perintah yang sama pada kedua sistem operasi:

```bash
docker ps -a --filter name=etc-sql
docker volume ls --filter name=etc-sql-data
```

Jika container `etc-sql` sudah ada dan setup SQL-nya berhasil, jalankan `docker start etc-sql`, lalu lanjut ke langkah 4. Jika volume bernama tepat `etc-sql-data` sudah ada, jangan menganggap pembuatan container akan mengganti password SQL. Gunakan password yang tersimpan pada volume tersebut atau ikuti [pemulihan setup gagal](#pemulihan-setup-sql-yang-gagal). Untuk komputer baru tanpa container/volume tersebut, lanjutkan di bawah.

Pilih password SQL **12–128 karakter**, berisi huruf besar, huruf kecil, angka, dan simbol. Untuk contoh penyusunan connection string pada panduan ini, hindari titik koma (`;`) dan tanda kutip (`'` / `"`). Simpan password tersebut untuk akses SQL berikutnya.

### Ubuntu / Bash

Jalankan **baris ini saja**, ketik password, lalu tekan Enter. Karakter password memang tidak terlihat. Jangan menempelkan blok berikutnya sebelum input password selesai.

```bash
read -r -s -p 'Password SQL Server baru: ' MSSQL_SA_PASSWORD
```

Setelah password masuk, jalankan:

```bash
printf '\n'
export MSSQL_SA_PASSWORD
docker run --name etc-sql \
  --env ACCEPT_EULA=Y \
  --env MSSQL_PID=Developer \
  --env MSSQL_SA_PASSWORD="${MSSQL_SA_PASSWORD:?Password SQL belum diisi}" \
  --publish 127.0.0.1:1433:1433 \
  --volume etc-sql-data:/var/opt/mssql \
  --detach mcr.microsoft.com/mssql/server:2022-latest
unset MSSQL_SA_PASSWORD
```

### Windows / PowerShell

```powershell
$secureSqlPassword = Read-Host 'Password SQL Server baru' -AsSecureString
$env:MSSQL_SA_PASSWORD = [System.Net.NetworkCredential]::new('', $secureSqlPassword).Password
if ($env:MSSQL_SA_PASSWORD.Length -lt 12 -or $env:MSSQL_SA_PASSWORD.Length -gt 128) {
    throw 'Password SQL harus 12–128 karakter. Masukkan ulang sebelum membuat container.'
}
docker run --name etc-sql --env ACCEPT_EULA=Y --env MSSQL_PID=Developer --env MSSQL_SA_PASSWORD --publish 127.0.0.1:1433:1433 --volume etc-sql-data:/var/opt/mssql --detach mcr.microsoft.com/mssql/server:2022-latest
Remove-Item Env:MSSQL_SA_PASSWORD
Remove-Variable secureSqlPassword
```

### Periksa hasil startup

Pada kedua sistem operasi:

```bash
docker ps -a --filter name=etc-sql
docker logs --tail 40 etc-sql
```

Tunggu sampai server siap menerima koneksi dan container tetap berstatus **Up**. Jika masih startup, ulangi pemeriksaan log. Jika berhenti atau ada `Password validation failed`, selesaikan error sebelum lanjut. Pesan siap dapat muncul sebelum setup pertama benar-benar selesai; tes login pada langkah 4 menjadi pemeriksaan berikutnya.

Volume menyimpan database saat container dihentikan. Jika port `1433` sudah digunakan server lain, gunakan host port lain pada `--publish`, misalnya `127.0.0.1:1434:1433`, lalu ubah `1433` menjadi `1434` pada connection string langkah 5. Port di dalam container tetap `1433`.

## 4. Buat database dan buktikan login SQL berhasil

Perintah berikut sama untuk Bash dan PowerShell. Masukkan **password SQL Server pada langkah 3** ketika muncul `Password:`:

```bash
docker exec -it etc-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -Q "IF DB_ID(N'ETC_Demo') IS NULL CREATE DATABASE ETC_Demo; SELECT name AS DatabaseSiap FROM sys.databases WHERE name = N'ETC_Demo';"
```

Hasil harus menampilkan **`ETC_Demo`**. Ini membuktikan akun SQL dapat login dan database tersedia. Jika muncul `Login failed`, jangan lanjut ke restore/init; periksa [error autentikasi](#mengatasi-error).

Nama database harus tepat `ETC_Demo` karena seed CLI membatasi targetnya. Membuat database belum membuat tabel aplikasi; tabel disiapkan pada langkah 8.

## 5. Atur koneksi aplikasi pada terminal yang sama

`ETC_CONNECTION_STRING` dibutuhkan oleh init, seed, login, daftar akun, dan fitur database lainnya. Aplikasi membaca environment proses, **tidak membaca `.env` otomatis**. Terminal baru perlu dikonfigurasi lagi. Jika aplikasi sudah terbuka sebelum environment diubah, tutup dan jalankan kembali.

Untuk jalur Docker baru pada panduan ini, ambil password konfigurasi container agar sama dengan password pada langkah 3, tanpa mengetiknya ulang atau menampilkannya.

### Ubuntu / Bash

```bash
etcSqlPassword="$(docker exec etc-sql sh -c 'printf "%s" "$MSSQL_SA_PASSWORD"')"
export ETC_CONNECTION_STRING="Server=127.0.0.1,1433;Database=ETC_Demo;User ID=sa;Password=${etcSqlPassword:?Password container kosong};Encrypt=true;TrustServerCertificate=true"
unset etcSqlPassword
```

### Windows / PowerShell

```powershell
$etcSqlPassword = docker exec etc-sql printenv MSSQL_SA_PASSWORD
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($etcSqlPassword)) {
    throw 'Password konfigurasi container tidak tersedia. Periksa container sebelum lanjut.'
}
$env:ETC_CONNECTION_STRING = "Server=127.0.0.1,1433;Database=ETC_Demo;User ID=sa;Password=$etcSqlPassword;Encrypt=true;TrustServerCertificate=true"
Remove-Variable etcSqlPassword
```

Cara ini memakai password environment saat container dibuat. Pada volume lama atau akun `sa` yang password-nya pernah diubah, nilai tersebut dapat berbeda dari password SQL sebenarnya. Jika init mendapat SQL `18456`, ikuti [pengaturan kredensial manual](#pengaturan-kredensial-manual) menggunakan password yang berhasil pada langkah 4; jangan reset database terlebih dahulu.

`TrustServerCertificate=true` digunakan untuk SQL lokal dengan sertifikat lokal pada demonstrasi ini. Jangan membagikan isi connection string karena mengandung password.

## 6. Restore paket

Dari folder utama yang berisi `Etc.slnx`:

```bash
dotnet restore
```

Pastikan restore selesai tanpa error sebelum melanjutkan. Perintah ini mengunduh paket NuGet; tidak membuat atau menguji database.

## 7. Build aplikasi

```bash
dotnet build
```

Pastikan muncul **`Build succeeded`**. Build mengompilasi kode, AXAML, dan resource aplikasi. Build yang berhasil belum membuktikan koneksi database benar.

## 8. Inisialisasi tabel aplikasi

```bash
dotnet run --project src/Etc.Desktop --no-build -- --init-db
```

Hasil yang diperlukan:

```text
Target SQL: 127.0.0.1,1433; Database: ETC_Demo.
Skema ETC siap.
```

Host/port mengikuti konfigurasi Anda. **Jangan lanjut ke seed jika belum muncul `Skema ETC siap.`** Perintah ini membuat/memperbarui tabel di database yang sudah ada; tidak membuat server atau database baru. CLI menampilkan target dan kode error tanpa mencetak password.

## 9. Seed data beserta akun demo

Seeder membuat akun mahasiswa demo dengan password bawaan **`Etc-Demo!2026`**. Anda tidak perlu memasukkan password demo saat setup. 

`ETC_DEMO_PASSWORD` hanya override opsional jika ingin memilih password demo sendiri, sepanjang 12–128 karakter. Jika sebelumnya pernah mengaturnya dan sekarang ingin memakai password bawaan, hapus override terlebih dahulu:

**Ubuntu / Bash:**

```bash
unset ETC_DEMO_PASSWORD
```

**Windows / PowerShell:**

```powershell
Remove-Item Env:ETC_DEMO_PASSWORD -ErrorAction SilentlyContinue
```

Kemudian jalankan seed dari terminal yang sama, pada kedua sistem operasi:

```bash
dotnet run --project src/Etc.Desktop --no-build -- --seed-demo
```

Hasil yang diperlukan:

```text
Target SQL: 127.0.0.1,1433; Database: ETC_Demo.
Data demonstrasi siap; password tidak ditampilkan.
```

Pada database kosong, seed memasukkan 3 akun demo, 8 rekrutmen, 7 lamaran, dan 15 mentor beserta foto. Seed ulang mempertahankan akun/password/rekrutmen yang sudah ada dan memperbarui mentor sumber berdasarkan NRP. Seed bukan reset database.

## 10. Buka aplikasi dan login

Setelah init dan seed berhasil:

```bash
dotnet run --project src/Etc.Desktop --no-build
```

Masuk menggunakan salah satu akun berikut. Untuk setup baru tanpa override password:

| Email | Password |
| --- | --- |
| `raka.demo@example.test` | `Etc-Demo!2026` |
| `nadia.demo@example.test` | `Etc-Demo!2026` |
| `dimas.demo@example.test` | `Etc-Demo!2026` |

Anda juga dapat membuat akun sendiri melalui **Buat akun**. Password akun baru ditentukan pada formulir. Jika menggunakan override pada seed pertama, gunakan password override tersebut. Jika akun demo pernah dibuat sebelumnya, password-nya tetap password saat seed pertama; seed ulang tidak menggantinya dengan password bawaan.

Setup komputer tersebut selesai setelah aplikasi dapat login dan menampilkan data. Data tersimpan pada SQL Server di komputer tersebut; menyalin source ke komputer lain tidak menyalin database. Lihat [skenario demonstrasi](docs/DEMO.md).

## Menjalankan setelah setup selesai

Untuk penggunaan berikutnya, lakukan urutan ini:

1. Buka Docker Desktop di Windows atau pastikan service Docker berjalan di Ubuntu.
2. Nyalakan SQL dan tunggu siap:

```bash
docker start etc-sql
docker ps --filter name=etc-sql
```

3. Di terminal yang akan menjalankan aplikasi, ulangi **[langkah 5](#5-atur-koneksi-aplikasi-pada-terminal-yang-sama)**. Ini diperlukan jika terminal baru dibuka. Jika password SQL pernah diubah, gunakan [kredensial manual](#pengaturan-kredensial-manual).
4. Dari folder utama, jalankan satu per satu:

```bash
dotnet restore
dotnet build
dotnet run --project src/Etc.Desktop
```

Untuk tiga perintah tanpa argumen project, dari folder utama masuk ke folder aplikasi terlebih dahulu:

```bash
cd src/Etc.Desktop
dotnet restore
dotnet build
dotnet run
```

Pilih salah satu lokasi menjalankan perintah. Jika sudah berada di `src/Etc.Desktop`, jangan menambahkan path relatif `--project src/Etc.Desktop` lagi. Kembali ke folder utama sebelum mengikuti perintah bagian lain panduan ini.

Init/seed tidak diperlukan setiap membuka aplikasi. `ETC_DEMO_PASSWORD` bersifat opsional untuk mengganti password bawaan saat akun demo pertama dibuat; login biasa memakai password yang diketik pada halaman login. Jika source berubah, build ulang; `--no-build` memakai executable hasil build sebelumnya.

## Alternatif SQL Server yang sudah terpasang

Jika memakai SQL Server/SQL Express lokal atau server pada mesin lain, ganti langkah 3–5 dengan konfigurasi berikut. Setelah itu lanjutkan **langkah 6–10** seperti biasa.

Pertama, buka klien SQL pada instance tujuan dengan akun yang memiliki izin membuat database, lalu jalankan:

```sql
IF DB_ID(N'ETC_Demo') IS NULL
    CREATE DATABASE ETC_Demo;
```

Akun aplikasi perlu memiliki izin membuka database, menjalankan migrasi schema, serta membaca/menulis data. Untuk SQL authentication, ikuti [kredensial manual](#pengaturan-kredensial-manual) dan ganti alamat/port sesuai server. `127.0.0.1` menunjuk komputer yang menjalankan aplikasi; untuk server lain gunakan alamat server yang dapat diakses dari komputer tujuan.

Untuk SQL Express pada Windows dengan Windows authentication, jalankan di PowerShell:

```powershell
$env:ETC_CONNECTION_STRING = 'Server=localhost\SQLEXPRESS;Database=ETC_Demo;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
```

Ganti `localhost\SQLEXPRESS` jika nama instance berbeda. Lanjutkan langkah 6–10; password akun demo otomatis memakai nilai bawaan jika tidak ada override.

### Pengaturan kredensial manual

Gunakan password SQL yang login-nya sudah terbukti berhasil. Contoh tetap memakai password tanpa titik koma atau tanda kutip sebagaimana langkah 3. Sesuaikan host, port, dan User ID jika bukan Docker lokal.

**Ubuntu / Bash:** jalankan baris input ini sendiri dan tekan Enter setelah mengetik password:

```bash
read -r -s -p 'Password SQL Server yang berlaku: ' etcSqlPassword
```

Kemudian:

```bash
printf '\n'
export ETC_CONNECTION_STRING="Server=127.0.0.1,1433;Database=ETC_Demo;User ID=sa;Password=${etcSqlPassword:?Password SQL belum diisi};Encrypt=true;TrustServerCertificate=true"
unset etcSqlPassword
```

**Windows / PowerShell:**

```powershell
$secureSqlPassword = Read-Host 'Password SQL Server yang berlaku' -AsSecureString
$etcSqlPassword = [System.Net.NetworkCredential]::new('', $secureSqlPassword).Password
if ([string]::IsNullOrWhiteSpace($etcSqlPassword)) {
    throw 'Password SQL belum diisi.'
}
$env:ETC_CONNECTION_STRING = "Server=127.0.0.1,1433;Database=ETC_Demo;User ID=sa;Password=$etcSqlPassword;Encrypt=true;TrustServerCertificate=true"
Remove-Variable etcSqlPassword, secureSqlPassword
```

## Mengatasi error

Jika tahap gagal, hentikan di tahap tersebut. Periksa exit code segera setelah perintah: `echo $?` pada Bash atau `$LASTEXITCODE` pada PowerShell. Nilai `0` berarti berhasil.

| Gejala | Tindakan |
| --- | --- |
| SDK tidak ditemukan atau tidak cocok | Pasang SDK .NET 10 sesuai `global.json`; periksa `dotnet --list-sdks`. |
| Docker tidak dapat diakses | Pastikan service/Docker Desktop berjalan dan user terminal memiliki akses Docker pada langkah 2. |
| Container/name sudah dipakai | Periksa langkah 3. Gunakan container yang sudah ada jika sehat; jangan menganggap membuat ulang container akan mengganti password volume lama. |
| Port 1433 sudah dipakai | Pilih host port lain saat membuat container dan sesuaikan port pada connection string. |
| `Password validation failed` / container berhenti | Pilih password SQL yang sesuai dan ikuti pemulihan di bawah untuk setup awal yang gagal. |
| `ETC_CONNECTION_STRING belum diatur pada terminal ini.` | Ulangi langkah 5 pada terminal yang menjalankan perintah. File `.env` tidak dimuat otomatis. |
| Format connection string tidak valid / Server atau Database belum diisi | Periksa parameter koneksi. Gunakan contoh langkah 5 atau kredensial manual sesuai instance Anda. |
| `SQL 18456` / `Login failed for user 'sa'` | Autentikasi ditolak. Periksa `docker logs --tail 40 etc-sql`. Jika log menyebut `Password did not match`, gunakan password yang tersimpan pada SQL, bukan password baru dari pembuatan ulang container dengan volume lama. |
| `SQL 4060` / database tidak dapat dibuka | Pastikan database dibuat pada server yang sama, namanya benar, dan akun SQL memiliki izin akses. |
| `Seed CLI hanya diizinkan pada database ETC_Demo.` | Nama database pada connection string harus tepat `ETC_Demo`. |
| `ETC_DEMO_PASSWORD harus 12–128 karakter.` | Override password demo terisi tetapi tidak valid. Hapus variabel sesuai langkah 9 untuk memakai password bawaan, atau atur override yang valid. |
| Akun demo tidak bisa login setelah seed ulang | Gunakan password saat akun tersebut pertama dibuat; seed ulang tidak mengganti password akun lama. |
| Buat akun menampilkan error koneksi | Pastikan langkah 8 berhasil; tutup aplikasi dan buka lagi dari terminal yang koneksinya sudah benar. |
| Invalid object/column name | Jalankan init setelah build terbaru agar schema diperbarui. |
| Pesan umum lama tanpa nomor SQL | Jalankan `dotnet build`, lalu ulangi perintah agar menggunakan diagnostik terbaru. |
| Couldn't find a project to run | Dari root gunakan `--project src/Etc.Desktop`, atau masuk ke folder aplikasi untuk `dotnet run` tanpa argumen. |
| Project test hilang saat build root | Sertakan folder `tests` sesuai solution, atau restore/build project aplikasi langsung. |
| Window tidak terbuka pada Linux | Jalankan dari sesi desktop dan periksa pustaka pada langkah 2. |

**Jika SQL berhasil diakses tetapi aplikasi masih mendapat SQL 18456**, catat output CLI dan target server/database. Tes koneksi dengan password konfigurasi container, tanpa menampilkannya, dapat dijalankan di Bash:

```bash
docker exec etc-sql sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -d ETC_Demo -C -b -Q "SELECT DB_NAME() AS DatabaseAktif;"'
```

Jika tes tersebut berhasil, ulangi langkah 5 untuk menyamakan koneksi terminal dengan kredensial container. Jika gagal, nilai environment container mungkin tidak cocok dengan password volume; gunakan kredensial manual yang benar. Jangan membagikan password, isi connection string, atau output environment container saat meminta bantuan.

### Pemulihan setup SQL yang gagal

Bagian ini untuk **percobaan setup awal yang ingin dimulai dengan database kosong**. Untuk database lama berisi data yang ingin dipakai, pulihkan kredensial instance tersebut. Mengganti environment `MSSQL_SA_PASSWORD` tidak mengganti password akun yang sudah tersimpan pada volume, sesuai [troubleshooting Microsoft](https://learn.microsoft.com/en-us/sql/linux/containers/troubleshoot).

Simpan container lama dengan nama lain. Perintah berikut sama pada Bash dan PowerShell; pastikan nama cadangan `etc-sql-gagal` belum dipakai:

```bash
docker stop etc-sql
docker rename etc-sql etc-sql-gagal
docker volume ls
```

Kemudian ikuti kembali langkah 3 untuk membuat container `etc-sql`, dengan dua penyesuaian:

1. Isi password SQL baru yang sesuai.
2. Ganti `etc-sql-data` pada parameter `--volume` menjadi **nama baru yang belum ada**, misalnya `etc-sql-data-baru`. Jangan gunakan ulang volume yang berisi setup lama.

Setelah container siap, lanjutkan **langkah 4–10 seluruhnya**, termasuk konfigurasi koneksi dan seed akun demo. Volume baru masih kosong. Container/volume lama tetap disimpan; data lama tidak dipindahkan otomatis.

## Memperbarui data mentor

Edit [tabel_mahasiswa.md](src/Etc.Desktop/Assets/tabel_mahasiswa.md) dan foto pada `src/Etc.Desktop/Assets/mentor/`. Nama file harus sesuai persis, termasuk huruf besar/kecil pada Ubuntu. Pertahankan NRP jika ingin memperbarui mentor yang sama; NRP berbeda dianggap identitas lain.

Atur koneksi seperti langkah 5, kemudian jalankan dari root. Password demo memakai nilai bawaan jika tidak ada override:

```bash
dotnet run --project src/Etc.Desktop -- --seed-demo
```

Perintah ini melakukan build; jangan tambahkan `--no-build` setelah mengubah tabel atau foto karena aset ditanam ke assembly. Foto sumber dipertahankan; salinan di memori diperkecil hingga sisi terpanjang 960 piksel sebelum disimpan ke SQL. Seed ulang mempertahankan password akun demo yang sudah ada.

Google Form default berasal dari baris `Link form:` pada tabel. `ETC_MENTOR_FORM_URL` dapat menggantinya; nilai kosong menonaktifkan pengajuan. Browser desktop diperlukan untuk membuka form dan portofolio. Respons Google Form tidak disimpan di aplikasi.

## Struktur dan pengujian

- `src/Etc.Desktop`: aplikasi, fitur, koneksi database, shared code, aset, dan lisensi font/ikon.
- `database`: SQL schema, katalog skill, dan migrasi yang ditanam saat build.
- `tests`: unit test dan integration test SQL; tetap disertakan untuk build solution.
- `tools/Etc.RenderChecks`: pemeriksaan UI headless; opsional untuk pemakaian aplikasi biasa.
- `global.json`, `Directory.Build.props`, `.csproj`, dan `packages.lock.json`: konfigurasi SDK, target build, dan dependensi.
- [PANDUAN_CODEBASE.md](PANDUAN_CODEBASE.md): penjelasan arsitektur, folder, dan alur kode.

Untuk unit test:

```bash
dotnet test tests/Etc.UnitTests
```

Integration test memerlukan `ETC_TEST_CONNECTION_STRING` ke database terpisah bernama `ETC_Test` atau `ETC_Test_...`. Fixture membuat/menginisialisasi database test dan menghapus data test saat skenario berjalan. **Jangan arahkan test ke database aplikasi.** Setelah environment test dikonfigurasi:

```bash
dotnet test Etc.slnx
```

Render checks memerlukan `ETC_CONNECTION_STRING`, menggunakan SQL demo serta Avalonia Headless/Skia. Password mengikuti nilai bawaan seeder; jika akun demo pada database tersebut dibuat dengan password lain, atur `ETC_DEMO_PASSWORD` sesuai password akun yang tersimpan:

```bash
dotnet run --project tools/Etc.RenderChecks -- docs/qa/screenshots
```

Aplikasi ini demonstrasi desktop tanpa API, admin, chat, pembayaran, atau sinkronisasi offline. Data tersimpan pada SQL Server; sesi login berakhir saat aplikasi ditutup. Font DM Sans dan ikon Fluent disertakan lokal beserta lisensinya. Jangan menyertakan kredensial privat ketika membagikan repository.
