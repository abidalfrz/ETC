
#set document(title: "PRD ETC - Elite TC Champions", author: "Tim pengembang ETC", description: "Kebutuhan produk desktop rekrutmen mahasiswa dan direktori mentor")
#set text(font: "Libertinus Serif", size: 10.5pt, fill: black, lang: "id")
#set par(justify: true, leading: 0.58em, spacing: 0.8em)
#set heading(numbering: "1.1", supplement: [Bagian])
#show heading.where(level: 1): set text(size: 15pt, weight: "bold", fill: black)
#show heading.where(level: 2): set text(size: 12pt, weight: "bold", fill: black)
#show heading.where(level: 3): set text(size: 10.5pt, weight: "bold", fill: black)
#show link: set text(fill: black)
#show raw: set text(size: 9pt, fill: black)
#set page(
  paper: "a4",
  margin: (top: 20mm, bottom: 19mm, left: 18mm, right: 18mm),
  header: align(right, text(size: 8pt, fill: black)[ETC | Product Requirements Document]),
  footer: context [
    #line(length: 100%, stroke: 0.4pt + black)
    #v(3pt)
    #align(center, text(size: 8pt, fill: black)[ETC | Draft kebutuhan produk | #counter(page).display("1 / 1", both: true)])
  ],
)

#let prd-table(columns, headers, cells) = {
  set text(size: 9pt, fill: black)
  set par(justify: false, leading: 0.45em, spacing: 0.5em)
  table(
    columns: columns,
    align: (left, top),
    inset: 5pt,
    stroke: 0.45pt + black,
    fill: none,
    table.header(repeat: true, ..headers.map(h => strong(h))),
    ..cells,
  )
}


#v(14mm)
#align(center)[
  #text(size: 28pt, weight: "bold")[ETC]
  #v(3mm)
  #text(size: 17pt, weight: "bold")[Elite TC Champions]
  #v(6mm)
  #text(size: 15pt, weight: "bold")[Product Requirements Document]
  #v(3mm)
  Sistem informasi Desktop untuk Rekrutmen Tim Mahasiswa Teknik Informatika ITS 
]
#v(10mm)

#prd-table((42mm, 1fr), ([Informasi], [Keterangan]), (
  [Nama produk], [ETC (Elite TC Champions)],
  [Jenis dokumen], [Product Requirements Document (PRD)],
  [Versi dan status], [1.0. Draft untuk peninjauan dan penyelarasan rilis demonstrasi. Belum merupakan persetujuan seluruh pemangku kepentingan.],
  [Tanggal penyusunan], [20 Oktober 2026],
  [Platform], [Desktop. Ubuntu x64 sebagai target demonstrasi utama; Windows x64 sebagai target tambahan yang memerlukan verifikasi native.],
  [Teknologi utama], [C\#, .NET 10, Avalonia, AXAML, MVVM, CommunityToolkit.Mvvm, SQL Server, dan ADO.NET melalui Microsoft.Data.SqlClient.],
  [Target pengguna], [Mahasiswa, dengan konteks sebagai pembuat rekrutmen atau pelamar. Mentor ditampilkan sebagai direktori, bukan peran akun tersendiri.],
  [Konteks penggunaan], [Demonstrasi tugas kuliah. Pengembangan dan verifikasi lintas komputer mengikuti kebutuhan demonstrasi, bukan asumsi layanan publik.],
  [Referensi desain], [Keputusan pengguna, kontrak layar, dan antarmuka Avalonia ETC terkini. Dokumen contoh hanya menjadi acuan struktur dan penyajian.],
  [Pemilik dan pengesah], [Perlu Keputusan: nama pemilik produk, penanggung jawab akademik, dan pihak yang mengesahkan rilis.],
))

#v(7mm)
Dokumen ini menetapkan kebutuhan, batas bisnis, pengalaman pengguna, rancangan data, arsitektur, dan kriteria verifikasi ETC. Dokumen digunakan oleh pemilik produk, desainer, developer, dan QA untuk menyepakati perilaku rilis demonstrasi dan menilai hasil implementasi.

#pagebreak()
#text(size: 16pt, weight: "bold")[Daftar isi]
#v(5mm)
#outline(title: none, depth: 2)
#pagebreak()

= Ringkasan Eksekutif

ETC membantu mahasiswa mengumumkan kebutuhan anggota tim lomba atau proyek, mencari posisi yang relevan, mengirim lamaran, dan mengikuti hasil seleksi. Pembuat rekrutmen dan pelamar menggunakan satu jenis akun mahasiswa; kewenangan ditentukan oleh kepemilikan data dan konteks tindakan.

Produk juga menyediakan direktori mentor mahasiswa berdasarkan data yang disiapkan pengelola demonstrasi. Pengajuan mentoring diteruskan ke Google Form melalui browser. ETC tidak mengambil atau menampilkan status respons Google Form.

Dokumen menggabungkan OUTLINE.md, keputusan pengguna berikutnya, serta aturan yang dapat ditelusuri pada codebase saat penyusunan. Informasi yang belum tersedia dinyatakan sebagai *Asumsi*, *Usulan*, atau *Perlu Keputusan*. PRD ini tidak menyatakan bahwa seluruh kriteria rilis telah diuji atau disahkan.

== Tujuan Produk

- Menyediakan informasi kegiatan, posisi, skill, tenggat, kuota, dan kontak dalam satu aplikasi.
- Membantu mahasiswa mengelola rekrutmen dan menyeleksi lamaran sesuai kewenangannya.
- Memberikan riwayat lamaran dan hasil seleksi yang dapat dibaca kembali.
- Membantu mahasiswa menemukan informasi mentor dan membuka formulir mentoring yang tersedia.
- Mendemonstrasikan rich UI desktop, MVVM, data binding, validasi, transaksi, serta persistensi SQL Server.

== Pernyataan Produk

Untuk mahasiswa yang membutuhkan anggota tim atau kesempatan bergabung dalam lomba dan proyek, ETC menyediakan aplikasi desktop dengan informasi posisi, pelamaran, dan seleksi dalam satu alur. Mahasiswa dapat merekrut pada satu kegiatan dan melamar pada kegiatan mahasiswa lain menggunakan akun yang sama.

= Latar Belakang dan Permasalahan

Kebutuhan awal yang dinyatakan pengguna adalah mempertemukan mahasiswa pembuat rekrutmen dengan mahasiswa pencari rekrutmen. Informasi kegiatan perlu menjelaskan posisi dan persyaratan sebelum mahasiswa memutuskan untuk melamar. Hasil seleksi perlu tersimpan agar pembuat dan pelamar dapat membaca status yang sama.

#prd-table((16mm, 1fr, 1fr), ([ID], [Permasalahan yang ditangani], [Tanggapan produk]), (
  [P-01], [Kebutuhan anggota tim perlu dijelaskan secara terstruktur.], [Rekrutmen memuat kegiatan, posisi, skill, kuota, tenggat, biaya, dan kontak.],
  [P-02], [Mahasiswa perlu memilah kesempatan yang relevan dan masih tersedia.], [Pencarian, filter, detail posisi, bookmark, dan alasan pendaftaran tidak tersedia.],
  [P-03], [Pembuat perlu meninjau pelamar tanpa kehilangan hubungan dengan posisi yang dilamar.], [Daftar pelamar per posisi, profil, pesan, serta keputusan penerimaan atau penolakan.],
  [P-04], [Pelamar perlu mengetahui hasil dan riwayat pengajuannya.], [Lamaran saya menampilkan status, waktu pengajuan, dan akses detail kegiatan.],
  [P-05], [Informasi mentor perlu dapat ditelusuri berdasarkan keahlian.], [Direktori, foto, pencarian skill, detail, dan tautan formulir eksternal.],
  [P-06], [Demonstrasi perlu dapat dijalankan pada komputer lain dengan konfigurasi yang jelas.], [Panduan README, schema tertanam, CLI init/seed, dan pemisahan kredensial SQL dari akun aplikasi.],
))

*Batas bukti masalah:* belum tersedia riset pengguna, angka prevalensi, ukuran pasar, atau pengukuran waktu kerja sebelum menggunakan ETC. Pernyataan di atas merupakan kebutuhan proyek, bukan hasil survei atau klaim peningkatan produktivitas.

= Sasaran, Indikator Keberhasilan, dan Batasan

== Sasaran dan indikator penerimaan

#prd-table((15mm, 39mm, 1fr, 32mm), ([ID], [Sasaran], [Indikator yang dapat diperiksa], [Bukti penerimaan]), (
  [G-01], [Rekrutmen dapat diselesaikan], [Pemilik membuat draft, mengisi posisi, mempublikasikan, dan menutup rekrutmen sesuai aturan.], [TC-18 sampai TC-25],
  [G-02], [Pelamaran dan seleksi konsisten], [Akun lain melamar; pemilik memutuskan; status terbaca kembali tanpa kuota berlebih.], [TC-26 sampai TC-34],
  [G-03], [Kepemilikan data terlindungi], [Akun yang tidak berwenang tidak dapat mengubah rekrutmen, membatalkan lamaran orang lain, atau memutuskan seleksi.], [TC-06, TC-29, TC-30, TC-50],
  [G-04], [Ringkasan dapat dipercaya], [Statistik akun sesuai query; nol tetap nol; bookmark dan kuota sesuai data tersimpan.], [TC-11, TC-16, TC-33],
  [G-05], [Informasi mentor dapat diakses], [Profil sumber, foto, skill, pencarian, pagination, dan tautan formulir ditampilkan sesuai konfigurasi.], [TC-35 sampai TC-38, TC-55],
  [G-06], [Setup dapat direproduksi], [Source bersih dapat di-restore, di-build, diinisialisasi, di-seed, dan dipakai login pada lingkungan yang ditetapkan.], [TC-46 sampai TC-48, TC-58],
  [G-07], [Interaksi dapat digunakan], [Alur inti dapat dijalankan dengan keyboard; form mempertahankan isian saat gagal; layout tidak memotong tindakan utama.], [TC-41 sampai TC-45, TC-56],
))

Indikator merupakan kriteria penerimaan rilis, bukan klaim hasil pengujian terkini. Target jumlah pengguna, persentase adopsi, atau waktu respons belum ditetapkan. Ukuran dataset benchmark dan ambang performa memerlukan KD-01 pada Bagian 18.

== Prioritas dan status kebutuhan

#prd-table((24mm, 1fr), ([Istilah], [Makna]), (
  [Must], [Wajib untuk ruang lingkup rilis demonstrasi dalam PRD ini. Kebutuhan harus memiliki kriteria penerimaan dan bukti verifikasi sebelum dinyatakan selesai.],
  [Should], [Disarankan untuk kualitas atau kesiapan lanjutan. Usulan yang belum disetujui tidak boleh diperlakukan sebagai fitur wajib yang sudah dijanjikan.],
  [Could], [Pilihan pengembangan berikutnya; tidak menjadi syarat rilis awal tanpa persetujuan pemilik produk.],
  [Perlu Keputusan], [Informasi atau kebijakan belum ditentukan. Dampak dan pihak yang perlu memutuskan dicatat pada Bagian 18.],
  [Asumsi], [Dasar kerja sementara yang dinyatakan secara terbuka dan perlu divalidasi jika konteks berubah.],
))

== Batasan proyek

ETC merupakan demonstrasi tugas kuliah, menggunakan satu jenis akun mahasiswa, tema terang saja, dan koneksi SQL Server. Pembaruan data dilakukan melalui pemuatan ulang; tidak ada jaminan pembaruan real-time. Layanan publik, pembayaran, komunikasi tim, dan operasional mentoring berada di luar lingkup awal. Jadwal, anggaran, nama institusi, serta rubrik penilaian memerlukan KD-03.

= Pengguna dan Hak Akses

#prd-table((30mm, 1fr, 1fr), ([Pengguna/konteks], [Kebutuhan dan hak akses], [Batas kewenangan]), (
  [Belum login], [Membaca onboarding dan menggunakan register/login.], [Tidak mengakses data pribadi, daftar terlindungi, atau operasi akun mahasiswa.],
  [Mahasiswa login], [Mengelola profil sendiri; membaca profil publik; mencari rekrutmen; mengelola bookmark dan lamaran sendiri; melihat mentor.], [Tidak membaca email login atau password melalui profil publik; tidak mengubah data milik akun lain.],
  [Pembuat rekrutmen], [Mahasiswa yang memiliki suatu rekrutmen; mengubah posting, melihat pelamar, memutuskan, dan menutup posting tersebut.], [Kewenangan berlaku pada rekrutmen miliknya; tidak dapat melamar posting sendiri.],
  [Pelamar], [Mahasiswa yang mengirim lamaran ke rekrutmen orang lain; membaca status dan membatalkan pengajuan yang masih menunggu.], [Tidak dapat memutuskan seleksi atau mengubah status akhir.],
  [Mentor pada direktori], [Objek informasi mahasiswa mentor yang disiapkan melalui sumber data.], [Tidak mempunyai jenis akun, portal, atau kewenangan mentor tersendiri dalam rilis awal.],
  [Penyiap demonstrasi], [Menyiapkan SQL, konfigurasi, sumber mentor, dan seed melalui alat pengembangan.], [Bukan peran admin pada aplikasi; tidak ada halaman pengelolaan pengguna atau mentor.],
))

Satu akun dapat menjadi pembuat pada rekrutmen A dan pelamar pada rekrutmen B. Otorisasi harus diperiksa di service sebelum membaca data privat atau melakukan perubahan; menyembunyikan tombol saja tidak cukup. Batas penemuan profil publik di luar konteks rekrutmen memerlukan KD-09.

= Ruang Lingkup Produk

== In Scope untuk rilis awal

#prd-table((35mm, 1fr, 28mm), ([Area], [Cakupan], [Kebutuhan terkait]), (
  [Akun dan profil], [Onboarding; register/login; konfirmasi logout; sesi; profil, foto, skill, dan profil publik.], [FR-01 sampai FR-08],
  [Penemuan kegiatan], [Dashboard pribadi; pencarian, filter, pagination, detail, dan bookmark.], [FR-09 sampai FR-13],
  [Pengelolaan rekrutmen], [Draft, posisi, kuota, publikasi, pembaruan terkendali, penutupan, dan penghapusan draft.], [FR-14 sampai FR-19],
  [Lamaran dan seleksi], [Pengajuan, aturan kelayakan operasional, pembatalan, peninjauan, keputusan, kuota atomik, dan riwayat.], [FR-20 sampai FR-26],
  [Direktori mentor], [Kartu seragam dengan foto, pencarian, pagination, detail, dan pembukaan Google Form.], [FR-27 sampai FR-30],
  [Kualitas interaksi], [Validasi, status UI, retry, keyboard, reflow desktop, waktu WIB, dan konfirmasi tindakan.], [FR-31 sampai FR-33; FR-36 sampai FR-39],
  [Setup dan persistensi], [Init schema, seed demo, panduan antar-komputer, dan data tetap tersedia setelah aplikasi ditutup.], [FR-34, FR-35],
))

== Out of Scope atau rilis berikutnya

#prd-table((39mm, 1fr, 28mm), ([Area], [Batas rilis awal], [Kedudukan]), (
  [Chat dan notifikasi eksternal], [Tidak ada chat, video call, email/push otomatis, atau komunikasi real-time.], [Out of scope; Could setelah disepakati],
  [Pencocokan otomatis], [Tidak ada ranking kecocokan atau penilaian skill berbasis AI. Seleksi dilakukan pembuat.], [Out of scope],
  [Operasional tim], [Tidak ada workspace tugas, pembagian pekerjaan, atau pelacakan proyek setelah seleksi.], [Out of scope],
  [Transaksi keuangan], [Biaya bersifat informasi; tidak ada pembayaran, penagihan, escrow, atau pencairan fee.], [Out of scope],
  [Operasional mentoring], [Tidak ada penjadwalan, chat mentor, riwayat sesi, atau sinkronisasi respons Google Form.], [Out of scope],
  [Administrasi platform], [Tidak ada panel admin, moderasi, verifikasi kampus, akun mentor khusus, atau pengelolaan katalog melalui UI.], [Out of scope],
  [Layanan publik], [Tidak ada backend API, distribusi kredensial untuk pengguna publik, multi-tenant, sinkronisasi offline, atau SLA publik.], [Perlu PRD lanjutan],
  [Akun lanjutan dan tema], [Pemulihan password melalui email, SSO, remember-me persisten, dan dark mode tidak termasuk.], [Out of scope; tema terang telah diputuskan],
))

= Struktur Navigasi dan Halaman

#prd-table((15mm, 34mm, 1fr, 34mm), ([ID], [Halaman], [Isi dan tindakan], [Akses]), (
  [PG-01], [Onboarding], [Penjelasan rekrutmen dan mentor; Masuk dan Buat akun.], [Belum login],
  [PG-02], [Login], [Email, password, validasi, dan akses register.], [Belum login],
  [PG-03], [Register], [Nama lengkap, email, password, dan akses login.], [Belum login],
  [PG-04], [Dashboard], [Empat statistik pribadi, rekrutmen dan lamaran terbaru, serta pintasan nyata.], [Mahasiswa login],
  [PG-05], [Cari rekrutmen], [Pencarian, kategori, posisi, skill, ketersediaan, hasil, dan pagination.], [Mahasiswa login],
  [PG-06], [Detail rekrutmen], [Pemilik, kegiatan, posisi/kuota, tenggat, jadwal, biaya, kontak, dan formulir lamaran.], [Mahasiswa; draft hanya pemilik],
  [PG-07], [Rekrutmen saya], [Posting milik akun, status, buat/edit, publikasi, pelamar, tutup, dan hapus draft.], [Pemilik data],
  [PG-08], [Form rekrutmen], [Kegiatan, editor posisi, jadwal/biaya, kontak, validasi, simpan draft dan publikasi.], [Pemilik data],
  [PG-09], [Pelamar], [Daftar per posisi, profil/pesan, kuota, Terima/Tolak, dan status final.], [Pemilik rekrutmen],
  [PG-10], [Lamaran saya], [Kegiatan/posisi, waktu, status, detail, dan pembatalan pending.], [Pemilik lamaran],
  [PG-11], [Bookmark], [Posting tersimpan, status terkini, buka detail, dan hapus bookmark.], [Pemilik bookmark],
  [PG-12], [Daftar mentor], [Foto, nama, angkatan/fokus, skill, ringkasan, pencarian, dan pagination.], [Mahasiswa login],
  [PG-13], [Detail mentor], [Identitas, foto, skill, pengalaman/pencapaian, portofolio, dan formulir.], [Mahasiswa login],
  [PG-14], [Profil saya], [Identitas, foto, angkatan, skill, bio, pengalaman, dan tautan.], [Pemilik profil],
  [PG-15], [Profil publik], [Nama, skill, pengalaman, foto, serta tautan tanpa email login/password.], [Mahasiswa login],
))

Menu utama memuat Dashboard, Cari rekrutmen, Rekrutmen saya, Lamaran saya, Bookmark, dan Mentor. Bagian bawah navbar menyediakan Profil saya dan Keluar. Detail/form diakses dari konteks daftar, bukan dijadikan menu utama tambahan. Pada jendela ringkas, menu menjadi flyout dengan kontrol buka/tutup dan Escape. Kembali harus memulihkan halaman, filter, dan posisi scroll yang relevan.

#pagebreak()
= Kebutuhan Fungsional

Kebutuhan berikut merupakan baseline rilis demonstrasi. Semua baris berprioritas Must karena menjabarkan alur utama dan kualitas yang sudah menjadi cakupan ETC; pengembangan tambahan tidak disisipkan sebagai kewajiban baru. Usulan Should terdapat pada kebutuhan nonfungsional dan keputusan terbuka.

#prd-table((15mm, 31mm, 1fr, 17mm), ([ID], [Fitur], [Persyaratan], [Prioritas]), (
  [FR-01], [Onboarding], [Sistem menjelaskan fungsi rekrutmen dan mentor serta menyediakan akses Masuk/Buat akun tanpa menu terlindungi yang tidak dapat digunakan.], [Must],
  [FR-02], [Registrasi], [Mahasiswa mendaftar dengan nama, email valid dan unik setelah normalisasi, serta password 12 sampai 128 karakter; sistem menyimpan hash.], [Must],
  [FR-03], [Login], [Sistem memverifikasi email/password, menolak kredensial salah dengan pesan aman, dan membuka sesi akun yang benar.], [Must],
  [FR-04], [Logout], [Sistem meminta konfirmasi Keluar; Batal/Escape mempertahankan sesi; persetujuan mengakhiri sesi dan kembali ke halaman akun.], [Must],
  [FR-05], [Sesi dan otorisasi], [Sistem membatasi halaman privat pada sesi aktif, memeriksa kepemilikan di service, dan mencegah hasil async akun lama mengisi tampilan akun baru.], [Must],
  [FR-06], [Profil sendiri], [Mahasiswa menyimpan nama, angkatan, skill, bio/pengalaman, foto, dan tautan yang valid hanya untuk akun sendiri.], [Must],
  [FR-07], [Kelengkapan profil], [Sebelum membuat rekrutmen atau melamar, sistem mensyaratkan nama, angkatan, dan minimal satu skill; registrasi awal dapat menghasilkan profil belum lengkap.], [Must],
  [FR-08], [Profil publik], [Sistem menampilkan informasi profil yang relevan tanpa email login, hash password, atau kontrol mengedit profil orang lain.], [Must],
  [FR-09], [Dashboard], [Sistem menghitung rekrutmen aktif milik akun, lamaran pending, lamaran diterima, dan bookmark; menampilkan ringkasan aktivitas serta pintasan ke data terkait.], [Must],
  [FR-10], [Cari dan filter], [Mahasiswa mencari judul/nama kegiatan serta memfilter kategori, posisi, skill, dan ketersediaan. Filter awal hanya menampilkan yang masih menerima lamaran.], [Must],
  [FR-11], [Pagination dan pemulihan], [Daftar memakai pagination 12 item; perubahan filter kembali ke halaman pertama; Kembali dari detail memulihkan konteks daftar yang relevan.], [Must],
  [FR-12], [Detail rekrutmen], [Sistem menampilkan kegiatan, pemilik, posisi/skill/kuota, tenggat, jadwal, mode/lokasi, biaya, kontak, dan tautan. Draft tidak dapat dibaca akun lain.], [Must],
  [FR-13], [Bookmark], [Mahasiswa menambah/menghapus bookmark sendiri tanpa duplikasi. Posting ditutup tetap dapat dibaca dari bookmark, tanpa membuka kembali pendaftaran.], [Must],
  [FR-14], [Draft rekrutmen], [Mahasiswa berprofil lengkap menyimpan dan mengubah draft miliknya; kolom publikasi dapat belum lengkap, tetapi format dan nilai yang sudah diisi tetap divalidasi.], [Must],
  [FR-15], [Posisi dan kuota], [Pemilik menetapkan nama posisi, kuota bilangan bulat positif, dan skill katalog. Publikasi mensyaratkan minimal satu posisi dengan skill.], [Must],
  [FR-16], [Publikasi], [Hanya pemilik dapat mempublikasikan draft yang lengkap, dengan tenggat di masa depan, mode/lokasi yang sesuai, kontak, dan persyaratan posisi.], [Must],
  [FR-17], [Pembaruan posting], [Pemilik dapat memperbarui informasi posting dengan waktu pembaruan. Setelah ada lamaran, posisi/skill/kuota terkunci; perubahan tenggat mengikuti BR-09.], [Must],
  [FR-18], [Penutupan], [Pemilik menutup rekrutmen dibuka setelah konfirmasi. Penutupan menghentikan lamaran baru, mempertahankan riwayat, dan tidak membuka kembali posting.], [Must],
  [FR-19], [Hapus draft], [Pemilik menghapus draft setelah konfirmasi. Posting terpublikasi tidak dihapus permanen melalui fitur ini.], [Must],
  [FR-20], [Kirim lamaran], [Mahasiswa memilih satu posisi pada rekrutmen orang lain dan mengirim pesan perkenalan serta portofolio opsional; sistem memeriksa ketersediaan kembali saat menyimpan.], [Must],
  [FR-21], [Batas pelamaran], [Sistem menolak lamaran ke posting sendiri, duplikasi satu mahasiswa per rekrutmen, posisi salah, posting ditutup, tenggat lewat, dan posisi penuh.], [Must],
  [FR-22], [Pembatalan], [Pelamar membatalkan lamaran pending miliknya setelah konfirmasi; status akhir tidak dapat dibatalkan atau dipakai untuk melamar ulang posting yang sama.], [Must],
  [FR-23], [Peninjauan pelamar], [Hanya pemilik rekrutmen membaca daftar pelamar per posisi beserta profil terbaru, pesan, portofolio, status, dan waktu.], [Must],
  [FR-24], [Keputusan seleksi], [Pemilik menerima atau menolak lamaran pending setelah konfirmasi; hasil akhir tersimpan dan dapat dibaca pelamar setelah memuat ulang.], [Must],
  [FR-25], [Integritas kuota], [Pemeriksaan kuota dan perubahan keputusan berjalan atomik agar dua penerimaan berdekatan tidak melebihi kapasitas posisi.], [Must],
  [FR-26], [Lamaran saya], [Sistem menampilkan riwayat milik akun aktif dengan posisi, kegiatan, waktu pengajuan, status, akses detail, dan aksi sesuai status.], [Must],
  [FR-27], [Direktori mentor], [Sistem menampilkan kartu seragam pada lebar jendela yang sama, foto tiap mentor sumber yang tersedia, identitas, skill, fokus, dan ringkasan pengalaman.], [Must],
  [FR-28], [Pencarian mentor], [Mahasiswa mencari nama/skill, memakai tombol skill sebagai pencarian, mereset pencarian, serta menavigasi pagination. Tidak ada tombol Muat ulang tambahan pada halaman mentor.], [Must],
  [FR-29], [Detail mentor], [Sistem menampilkan foto dan informasi sumber lengkap, memberi keterangan jika pengalaman kosong, serta menyediakan portofolio hanya bila tersedia dan valid.], [Must],
  [FR-30], [Form mentoring], [Tombol Butuh mentor? membuka Google Form terkonfigurasi di browser. URL kosong menonaktifkan tindakan dengan alasan; ETC tidak mengklaim pengajuan tersimpan.], [Must],
  [FR-31], [Validasi input], [Sistem memvalidasi kolom wajib, batas panjang, email, angkatan, tanggal, biaya, skill, foto, serta URL HTTP/HTTPS pada batas service dan input UI.], [Must],
  [FR-32], [Loading dan kegagalan], [Sistem menyediakan loading/empty/error/sukses; mencegah pengiriman ganda; mempertahankan isian saat gagal; menyediakan retry yang relevan tanpa mengklaim data baru tersimpan.], [Must],
  [FR-33], [Navigasi aksesibel], [Label input, fokus terlihat, Tab/Shift+Tab/Enter/Escape, menu ringkas, dan reflow desktop memungkinkan alur inti dijalankan tanpa mouse.], [Must],
  [FR-34], [Init dan seed], [CLI menginisialisasi schema pada database yang sudah ada; seed hanya menerima ETC_Demo, memakai password demo bawaan jika tidak ada override, dan menampilkan diagnostik aman.], [Must],
  [FR-35], [Persistensi dan sumber], [Data tersimpan bertahan setelah aplikasi ditutup; seed idempotent mempertahankan data manual/akun lama dan memperbarui mentor sumber berdasarkan NRP.], [Must],
  [FR-36], [Waktu], [Sistem menyimpan waktu transaksi dalam UTC dan menampilkan waktu relevan dengan label WIB, termasuk tenggat, publikasi, pembaruan, serta pengajuan.], [Must],
  [FR-37], [Status dan alasan], [Sistem membedakan status rekrutmen dari ketersediaan posisi, menampilkan alasan tenggat lewat/penuh/ditutup, dan tidak memakai warna sebagai satu-satunya informasi.], [Must],
  [FR-38], [Visual konsisten], [Aplikasi hanya memakai tema terang, permukaan daftar opaque yang terpisah dari kanvas, ikon berlabel, serta teks/ikon profil biru dan keluar merah pada latar navbar transparan.], [Must],
  [FR-39], [Konfirmasi dan umpan balik], [Tindakan yang memerlukan konfirmasi, termasuk publikasi, penutupan, hapus draft, keputusan seleksi, pembatalan, dan logout, mempunyai dialog jelas serta umpan balik hasil. Aktivitas penyimpanan pada @notifikasi-sukses menampilkan notifikasi sukses setelah berhasil disimpan; pesan mengikuti perpindahan halaman dan dapat ditutup. Penyimpanan gagal atau konfirmasi dibatalkan tidak menghasilkan notifikasi sukses.], [Must],
))

== Aturan bisnis dan transisi status

#prd-table((15mm, 1fr), ([ID], [Aturan]), (
  [BR-01], [Satu jenis akun mahasiswa. Kewenangan pembuat/pelamar diturunkan dari pemilik rekrutmen dan pemilik lamaran.],
  [BR-02], [Rekrutmen mengikuti Draft -> Dibuka -> Ditutup. Tidak ada pembukaan kembali atau penghapusan permanen posting terpublikasi pada rilis awal.],
  [BR-03], [Lamaran mengikuti Menunggu -> Diterima, Ditolak, atau Dibatalkan. Status akhir tidak dapat diubah melalui alur rilis awal.],
  [BR-04], [Satu mahasiswa memiliki paling banyak satu lamaran per rekrutmen, termasuk setelah ditolak/dibatalkan; hanya satu posisi dipilih.],
  [BR-05], [Pembuat tidak melamar posting sendiri. Profil wajib lengkap sebelum menyimpan rekrutmen atau mengirim lamaran.],
  [BR-06], [Pengajuan baru hanya sah bila status Dibuka, waktu sekarang lebih kecil daripada tenggat, dan accepted count posisi lebih kecil daripada kapasitas. Tepat pada tenggat pendaftaran berhenti.],
  [BR-07], [Tenggat lewat tidak otomatis mengubah status tersimpan menjadi Ditutup. Posisi penuh tidak menutup posisi lain yang masih tersedia.],
  [BR-08], [Begitu terdapat lamaran, nama/daftar posisi, skill, dan kapasitas terkunci. Informasi kegiatan/kontak tetap dapat diperbarui sesuai validasi.],
  [BR-09], [Pada posting terpublikasi, tenggat hanya dapat diubah jika rekrutmen masih Dibuka, tenggat lama belum lewat, dan masih ada posisi dengan kuota tersedia. Tenggat baru harus valid; lamaran lama tidak dihapus. Pengisian tenggat pada draft mengikuti validasi draft/publikasi.],
  [BR-10], [Pemilik dapat memutuskan lamaran yang sudah masuk setelah posting ditutup atau tenggat lewat; penerimaan tetap mematuhi kuota. Lamaran yang belum diputuskan tetap Menunggu.],
  [BR-11], [Kuota menghitung lamaran Diterima. Pemeriksaan dan penerimaan menggunakan transaksi serta penguncian rekrutmen; kapasitas tidak boleh terlampaui.],
  [BR-12], [Skill menjadi informasi dan filter posisi. Rilis awal tidak memberi skor atau jaminan kecocokan otomatis; pembuat menilai pelamar secara manual.],
  [BR-13], [Biaya Gratis disimpan sebagai nol; Belum diketahui tidak dianggap nol; Berbayar memerlukan nilai positif dan keterangan peruntukan. Tidak ada transaksi pembayaran.],
  [BR-14], [Profil publik memakai profil mahasiswa terkini, bukan snapshot saat melamar. Email login dan hash password tidak termasuk proyeksi publik.],
  [BR-15], [Bookmark unik per mahasiswa/rekrutmen. Menghapus bookmark tidak menghapus posting; posting ditutup tetap dapat dibaca sesuai hak akses.],
  [BR-16], [Mentor sumber berasal dari tabel/foto yang diberikan. Tidak ada rating, status online, jumlah mentee, atau klaim verifikasi yang dibuat tanpa sumber.],
  [BR-17], [Google Form dan portofolio dibuka eksternal. Tidak ada sinkronisasi respons atau pencatatan keberhasilan mentoring di ETC.],
  [BR-18], [Seed pertama tanpa override membuat akun demo dengan password Etc-Demo!2026. Seed ulang tidak mengganti password akun yang sudah ada; kredensial SQL tetap terpisah.],
))

#pagebreak()
= User Stories dan Kriteria Penerimaan

Kriteria di bawah harus diuji pada kondisi berhasil dan gagal yang relevan. Kode TC merujuk pada Bagian 16. Setiap story memetakan kebutuhan fungsional agar perubahan scope dapat dilacak.

#prd-table((15mm, 1fr, 1.4fr, 26mm), ([ID], [User story], [Kriteria penerimaan], [Keterlacakan]), (
  [US-01], [Sebagai mahasiswa baru, saya ingin memahami produk dan mendaftar agar mempunyai akun.], [AC-01a: onboarding memberi akses akun yang bekerja. AC-01b: data valid menghasilkan satu akun; email duplikat setelah normalisasi ditolak. AC-01c: password tidak disimpan sebagai teks biasa.], [FR-01, FR-02; TC-01, TC-02, TC-03],
  [US-02], [Sebagai mahasiswa, saya ingin login dan hanya mengakses data yang berwenang saya gunakan.], [AC-02a: kredensial benar membuka sesi yang sesuai; salah ditolak. AC-02b: halaman privat dan service menolak akses tanpa sesi/kepemilikan. AC-02c: pergantian akun tidak memulihkan hasil async akun lama.], [FR-03, FR-05; TC-04, TC-05, TC-06, TC-53],
  [US-03], [Sebagai mahasiswa, saya ingin mengonfirmasi logout agar tidak keluar tanpa sengaja.], [AC-03a: dialog menampilkan Keluar dan Batal. AC-03b: Batal/Escape tidak mengubah sesi. AC-03c: persetujuan mengakhiri sesi serta membersihkan state akun.], [FR-04; TC-07],
  [US-04], [Sebagai mahasiswa, saya ingin melengkapi profil agar dapat merekrut atau melamar.], [AC-04a: nama, angkatan, dan minimal satu skill wajib. AC-04b: foto/tautan yang tidak valid ditolak dengan isian tetap tersedia. AC-04c: profil belum lengkap menghalangi buat rekrutmen dan lamaran dengan arahan yang jelas.], [FR-06, FR-07; TC-08, TC-09, TC-40],
  [US-05], [Sebagai mahasiswa, saya ingin membaca profil publik untuk menilai pembuat atau pelamar.], [AC-05a: profil menampilkan nama, skill, pengalaman, dan foto/tautan yang tersedia. AC-05b: email login/password tidak muncul. AC-05c: profil orang lain tidak dapat diedit.], [FR-08; TC-10],
  [US-06], [Sebagai mahasiswa, saya ingin membaca ringkasan aktivitas akun saya.], [AC-06a: empat statistik sesuai query akun aktif. AC-06b: data kosong menampilkan nol dan state terkait. AC-06c: pintasan membuka daftar yang benar; ringkasan tidak memuat data akun lain.], [FR-09; TC-11],
  [US-07], [Sebagai pencari tim, saya ingin mencari dan memfilter posisi yang tersedia.], [AC-07a: hasil mengikuti kombinasi kata kunci/kategori/skill/posisi/ketersediaan. AC-07b: filter awal OnlyAvailable aktif. AC-07c: hasil dipaginasi 12 item, reset kembali halaman pertama, dan tanpa hasil mempunyai pesan serta Hapus filter. AC-07d: Kembali dari detail memulihkan halaman, filter, dan scroll daftar.], [FR-10, FR-11; TC-12, TC-13],
  [US-08], [Sebagai calon pelamar, saya ingin membaca detail dan alasan ketersediaan sebelum melamar.], [AC-08a: tenggat pendaftaran terpisah dari jadwal kegiatan. AC-08b: posisi/skill/kuota serta waktu WIB terlihat. AC-08c: penuh, tenggat lewat, dan ditutup mempunyai alasan; draft akun lain tidak terbaca.], [FR-12, FR-36, FR-37; TC-14, TC-15, TC-52],
  [US-09], [Sebagai mahasiswa, saya ingin menyimpan posting untuk dibaca kembali.], [AC-09a: simpan berulang tidak membuat duplikasi. AC-09b: hapus bookmark hanya menghapus relasi milik saya. AC-09c: posting ditutup tetap dapat dibuka tanpa mengaktifkan lamaran.], [FR-13; TC-16, TC-17],
  [US-10], [Sebagai pembuat, saya ingin menyimpan draft dan mengatur posisi sebelum publikasi.], [AC-10a: draft tidak muncul bagi akun lain. AC-10b: draft boleh belum lengkap; nilai yang diisi tetap valid. AC-10c: kapasitas posisi positif dan skill berasal dari katalog.], [FR-14, FR-15; TC-18, TC-19, TC-54],
  [US-11], [Sebagai pembuat, saya ingin mempublikasikan rekrutmen yang lengkap.], [AC-11a: hanya pemilik dapat publikasi. AC-11b: judul, kegiatan, deskripsi, kontak, tenggat, mode/lokasi, posisi dan skill memenuhi validasi. AC-11c: publikasi berhasil muncul sesuai filter pencarian.], [FR-16; TC-20, TC-21],
  [US-12], [Sebagai pembuat, saya ingin memperbarui informasi tanpa merusak lamaran yang sudah ada.], [AC-12a: setelah ada lamaran, perubahan posisi/skill/kuota ditolak. AC-12b: deskripsi/kontak valid dapat diperbarui dengan waktu pembaruan. AC-12c: perubahan tenggat hanya sah sesuai BR-09.], [FR-17; TC-22, TC-23],
  [US-13], [Sebagai pembuat, saya ingin menutup posting atau menghapus draft yang tidak digunakan.], [AC-13a: penutupan menolak lamaran baru tetapi mempertahankan riwayat. AC-13b: pembatalan dialog tidak mengubah data. AC-13c: hanya draft milik sendiri dapat dihapus; posting terpublikasi ditolak.], [FR-18, FR-19; TC-24, TC-25],
  [US-14], [Sebagai pelamar, saya ingin mengirim satu lamaran ke posisi yang tersedia.], [AC-14a: lamaran valid tersimpan Menunggu. AC-14b: posting sendiri, duplikasi, posisi salah, penuh, lewat tenggat, dan ditutup ditolak. AC-14c: validasi service tetap bekerja jika kondisi berubah setelah detail dibuka.], [FR-20, FR-21; TC-26, TC-27, TC-28],
  [US-15], [Sebagai pelamar, saya ingin membatalkan pengajuan yang belum diputuskan.], [AC-15a: hanya pending milik sendiri dapat dibatalkan. AC-15b: persetujuan menghasilkan Dibatalkan; Batal/Escape tidak mengubah data. AC-15c: pembatalan tidak mengizinkan pengajuan kedua pada posting yang sama.], [FR-22; TC-29],
  [US-16], [Sebagai pembuat, saya ingin meninjau pelamar berdasarkan posisi.], [AC-16a: hanya pemilik dapat membaca daftar pelamar. AC-16b: filter posisi, pagination, pesan, profil terbaru, dan status sesuai data. AC-16c: layout ringkas memungkinkan kembali ke daftar tanpa kehilangan konteks.], [FR-23; TC-30],
  [US-17], [Sebagai pembuat, saya ingin menetapkan hasil seleksi tanpa melampaui kuota.], [AC-17a: pending dapat diterima/ditolak dengan konfirmasi. AC-17b: status final tidak dapat diubah. AC-17c: pada dua penerimaan untuk satu slot tersisa, maksimal satu berhasil dan accepted count tidak melebihi kapasitas.], [FR-24, FR-25; TC-31, TC-32, TC-33],
  [US-18], [Sebagai pelamar, saya ingin membaca riwayat dan hasil seleksi saya.], [AC-18a: daftar hanya milik akun aktif dan menampilkan posisi, kegiatan, waktu serta status. AC-18b: hasil terbaru terlihat setelah pemuatan ulang. AC-18c: hanya pending menawarkan pembatalan.], [FR-26; TC-34],
  [US-19], [Sebagai mahasiswa, saya ingin mencari mentor berdasarkan nama atau keahlian.], [AC-19a: setiap profil sumber yang memiliki foto menampilkan foto. AC-19b: ukuran kartu konsisten pada lebar yang sama dan teks tetap utuh. AC-19c: tombol skill mencari, reset bekerja, dan pagination menjaga hasil yang benar tanpa tombol Muat ulang.], [FR-27, FR-28; TC-35, TC-36],
  [US-20], [Sebagai mahasiswa, saya ingin membaca detail mentor dan mengakses formulir pengajuan.], [AC-20a: detail mengikuti data sumber, tanpa klaim tambahan. AC-20b: URL valid membuka browser. AC-20c: URL kosong/nonvalid menampilkan alasan; tidak ada klaim respons formulir tersimpan di ETC.], [FR-29, FR-30; TC-37, TC-38],
  [US-21], [Sebagai pengguna form, saya ingin mengetahui input yang salah sebelum menyimpan.], [AC-21a: kesalahan wajib/format/panjang ditunjukkan dekat input atau ringkasan. AC-21b: URL berbahaya dan foto di luar batas ditolak. AC-21c: service menolak ID skill dan nilai enum yang tidak valid.], [FR-31; TC-39, TC-40, TC-54],
  [US-22], [Sebagai pengguna, saya ingin dapat melanjutkan setelah operasi gagal.], [AC-22a: loading mempunyai teks dan aksi ganda dicegah. AC-22b: gagal simpan mempertahankan isian; retry tersedia sesuai operasi. AC-22c: gagal refresh tidak menyamarkan data lama sebagai data baru.], [FR-32; TC-41, TC-42],
  [US-23], [Sebagai pengguna desktop, saya ingin menggunakan tampilan terang dengan keyboard pada ukuran jendela yang didukung.], [AC-23a: label dan ikon konsisten; profil biru dan keluar merah pada latar transparan. AC-23b: keyboard/fokus bekerja dan status tidak mengandalkan warna. AC-23c: reflow menjaga tindakan utama dan kontras pada lebar yang ditetapkan. AC-23d: reduced motion mengurangi gerak tanpa menghilangkan fungsi atau informasi.], [FR-33, FR-38; TC-43, TC-44, TC-45, TC-56],
  [US-24], [Sebagai penyiap demonstrasi, saya ingin menjalankan aplikasi pada komputer lain tanpa konfigurasi pribadi pembuat.], [AC-24a: README memandu SQL, koneksi, restore/build/init/seed/run berurutan. AC-24b: seed default tidak meminta password demo tambahan. AC-24c: seed ulang mempertahankan akun/data manual; data bertahan setelah restart.], [FR-34, FR-35; TC-46, TC-47, TC-48, TC-55, TC-58],
  [US-25], [Sebagai mahasiswa, saya ingin mengonfirmasi tindakan penting dan mengetahui hasilnya.], [AC-25a: dialog menyebut objek/tindakan serta menyediakan Batal. AC-25b: pembatalan tidak menulis perubahan. AC-25c: aktivitas pada @notifikasi-sukses menampilkan pesan yang menyebut tindakan setelah penyimpanan berhasil; kesalahan tidak membocorkan rahasia. AC-25d: banner tetap terlihat saat konten digulir atau berpindah halaman dalam sesi yang sama. AC-25e: tombol Tutup dapat digunakan dengan keyboard dan menghilangkan pesan. AC-25f: penyimpanan gagal atau konfirmasi dibatalkan tidak menampilkan pesan sukses untuk tindakan tersebut.], [FR-39; TC-49, TC-51, TC-60, TC-61, TC-62],
))

#pagebreak()
= Kebutuhan UI/UX dan Design System

== Gaya visual, warna, dan tipografi

Aplikasi mengikuti arah visual ETC sebagai tempat kerja mahasiswa untuk membentuk tim. Konten kegiatan, posisi, kuota, dan status lebih diutamakan daripada dekorasi. Keputusan tampilan bersumber dari DESIGN.md dan klarifikasi pengguna; bukan dari warna atau komponen produk contoh.

#prd-table((32mm, 1fr), ([Aspek], [Ketentuan aplikasi]), (
  [Tema], [Hanya tema terang. Tidak ada toggle dark mode atau pewarisan tema sistem yang mengubah tampilan.],
  [Warna dasar], [Navy `#0B1F3A`, kuning aksen `#F8C832`, kanvas `#F1F4F8`, dan permukaan putih `#FFFFFF`. Nilai ini merupakan spesifikasi UI, bukan warna cetak PRD.],
  [Navbar akun], [Profil saya menggunakan teks/ikon orang biru `#1D4ED8`; Keluar menggunakan teks/ikon keluar merah `#B91C1C`. Latar normal transparan seperti menu lain; hover netral dan fokus terlihat.],
  [Warna status], [Diterima, Ditolak, Menunggu, dan Dibatalkan selalu mempunyai label teks. Warna semantik membantu pembacaan, bukan menjadi satu-satunya pembeda.],
  [Tipografi], [DM Sans dibundel lokal untuk UI. Hierarki awal: judul 32 DIP, section 18, isi 14, metadata 12, dan angka statistik 24. Perubahan harus konsisten melalui token.],
  [Ikon], [Fluent System Icons Regular, umumnya 16 DIP pada navigasi/baris. Ikon mendukung label dan tidak menggantikan nama tindakan. Lisensi aset tetap disertakan.],
  [Permukaan daftar], [Kartu/daftar rekrutmen, posting sendiri, lamaran, dan bookmark memakai permukaan opaque yang jelas berbeda dari kanvas. Tidak ada baris transparan yang menyatu tanpa batas konteks.],
  [Motion], [Gerak singkat membantu hover/press atau perubahan konteks. ENERGY 2, RHYTHM 2, MOTION 1 menjadi pedoman; reduced motion dihormati. Tidak ada animasi dekoratif yang menghalangi tugas.],
))

== Layout, komponen, dan konsistensi

#prd-table((34mm, 1fr), ([Komponen/area], [Perilaku yang diharapkan]), (
  [Shell], [Sidebar berlabel, top bar, area konten, konteks akun, dan Kembali. Menu akun berada di bawah; detail/form mengikuti konteks navigasi asal.],
  [Dashboard], [Empat statistik yang dapat ditindaklanjuti, lamaran terbaru dan rekrutmen sendiri. Tidak ada grafik atau angka platform yang dibuat tanpa kebutuhan/data.],
  [Daftar], [Kegiatan, posisi, skill, tenggat, kuota, dan status dapat dipindai. Aksi mempunyai label. Pagination dan hasil kosong mempunyai konteks yang jelas.],
  [Kartu mentor], [Pada lebar yang sama, ukuran kartu seragam dan footer sejajar. Tinggi area identitas/skill/ringkasan diselaraskan tanpa memotong nama atau isi penting.],
  [Form], [Kolom wajib ditandai; tenggat pendaftaran dibedakan dari tanggal kegiatan. Error dekat input, nilai tersimpan dibedakan dari perubahan yang belum tersimpan.],
  [Dialog], [Judul/tindakan jelas, Batal tersedia dan mendapat fokus awal untuk tindakan berisiko. Escape membatalkan; konfirmasi tidak boleh dijalankan ganda.],
  [Token desain], [Warna, spasi, radius, ukuran huruf dan motion dikelola terpusat. Dua tombol akun tetap konsisten dengan ukuran dan jarak menu lain.],
))

== Status UI dan penanganan kegagalan

#prd-table((27mm, 1fr, 47mm), ([Status], [Informasi yang harus terlihat], [Tindakan]), (
  [Memuat], [Teks memuat/menyimpan; progres tidak mengarang persentase.], [Kontrol submit mencegah eksekusi ganda.],
  [Kosong], [Pesan spesifik sesuai daftar: belum ada posting, lamaran, bookmark, pelamar, atau hasil pencarian.], [Aksi relevan seperti Cari rekrutmen, Buat rekrutmen, atau Hapus filter.],
  [Gagal membaca], [Pesan aman dan konteks data yang gagal; data lama tidak dinyatakan terbaru.], [Coba lagi atau pemuatan ulang yang relevan.],
  [Gagal menyimpan], [Penjelasan bahwa perubahan belum tersimpan; isian tetap tersedia.], [Perbaiki input atau ulangi simpan.],
  [Sukses], [Banner Berhasil menyebut tindakan setelah perubahan tersimpan; daftar terkait dimuat ulang bila perlu. Perilaku mengikuti @notifikasi-sukses.], [Baca hasil, lanjutkan ke halaman terkait, atau pilih Tutup.],
  [Tidak tersedia], [Alasan seperti posisi penuh, tenggat lewat, formulir belum tersedia, profil belum lengkap, atau akses ditolak.], [Disabled dengan alasan atau arahkan ke langkah perbaikan.],
))

=== Notifikasi keberhasilan aktivitas <notifikasi-sukses>

Notifikasi sukses memberikan kepastian bahwa perubahan telah tersimpan. Ketentuan ini merupakan bagian dari FR-39 dan berlaku pada aktivitas berikut.

#prd-table((36mm, 1fr), ([Aspek], [Persyaratan]), (
  [Aktivitas pemicu], [Pendaftaran akun; simpan profil; simpan/hapus bookmark dari pencarian, detail, atau daftar bookmark; kirim/batalkan lamaran; simpan draft; publikasikan/perbarui/tutup rekrutmen; hapus draft; serta terima/tolak pelamar.],
  [Waktu kemunculan], [Pesan muncul setelah service mengonfirmasi penyimpanan berhasil. Selama operasi berlangsung, ketika penyimpanan gagal, atau ketika pengguna membatalkan konfirmasi, tidak ada pesan sukses untuk tindakan tersebut.],
  [Isi dan tampilan], [Banner bersama di bawah bar atas menggunakan label Berhasil dan pesan singkat yang menyebut tindakan. Warna hijau SuccessSoft/SuccessInk mengikuti design system aplikasi; makna tetap dijelaskan melalui teks.],
  [Perpindahan halaman], [Banner berada di luar area scroll dan tetap terlihat saat konten digulir. Pesan mengikuti perpindahan halaman dalam sesi yang sama, termasuk dari form ke detail setelah penyimpanan, dan dibersihkan saat logout.],
  [Menutup pesan], [Tombol Tutup berlabel aksesibel dapat digunakan dengan mouse atau keyboard. Pesan tidak hilang otomatis dan tidak mengambil fokus; setelah ditutup, pesan tidak muncul kembali hanya karena navigasi Kembali.],
  [Pemuatan ulang gagal], [Jika penyimpanan sudah berhasil tetapi pembacaan data terbaru gagal, keberhasilan simpan dan error pemuatan ditampilkan terpisah. Coba lagi mengulang pembacaan tanpa mengulangi penyimpanan yang sudah berhasil.],
))

== Aksesibilitas dan responsivitas desktop

Label input dan nama aksesibel tombol harus jelas; placeholder tidak menggantikan label. Tab/Shift+Tab mengikuti urutan kerja, Enter menjalankan tindakan yang relevan, dan Escape menutup dialog/menu. Fokus harus tetap terlihat pada teks/ikon berwarna, termasuk navbar akun. Kontras minimum: 4,5:1 untuk teks biasa serta 3:1 untuk teks besar dan indikator kontrol yang relevan. Pengujian tidak boleh hanya mengandalkan penilaian mata.

#prd-table((35mm, 1fr), ([Rentang/ketentuan], [Perilaku]), (
  [Minimum 800 x 560 DIP], [Tindakan inti tidak terpotong; konten panjang memakai scroll vertikal. Bukan komitmen dukungan ponsel.],
  [Lebar di bawah 1100 DIP], [Navbar menjadi flyout; konten detail/form ditumpuk dan urutan keputusan tetap jelas.],
  [1100 sampai 1399 DIP], [Sidebar terlihat, konten menyesuaikan ruang yang tersedia tanpa memaksa panel sempit.],
  [Mulai 1400 DIP], [Dashboard/detail dapat memakai dua kolom sesuai kepadatan isi.],
  [Direktori mentor], [Dua kolom saat lebar kanvas minimal 920 DIP; satu kolom jika lebih sempit.],
  [Verifikasi layout], [Periksa 1440 x 900, 1024 x 768, dan 800 x 560 DIP; cek skala 100%/150% pada OS sasaran. Hasil headless bukan pengganti bukti GUI native.],
))

= Kebutuhan Nonfungsional

#prd-table((17mm, 26mm, 1fr, 17mm), ([ID], [Kategori], [Persyaratan dan metode pemeriksaan], [Prioritas]), (
  [NFR-01], [Performa UI], [Operasi I/O menggunakan async/cancellation yang sesuai; interaksi dan indikator tetap berjalan saat database lambat. Verifikasi TC-41.], [Must],
  [NFR-02], [Performa data], [Daftar dipaginasi; ukuran halaman awal 12. Target latensi dan volume benchmark belum ditetapkan, mengikuti KD-01 dan TC-59. Kewajiban baris ini berlaku pada pagination, bukan SLA yang belum disepakati.], [Must],
  [NFR-03], [Keamanan akun], [Password disimpan dengan PasswordHasher berbasis Identity, bukan plaintext; kredensial salah tidak membentuk sesi. TC-03, TC-05, TC-46.], [Must],
  [NFR-04], [Keamanan query], [Input query menggunakan parameter. ID, enum, URL, dan nilai bisnis divalidasi; tidak membangun SQL dari input pengguna tanpa pembatasan. TC-39, TC-50, TC-54.], [Must],
  [NFR-05], [Otorisasi], [Akses data privat dan perubahan diperiksa di service terhadap sesi/kepemilikan, termasuk panggilan yang melewati UI. TC-06, TC-29, TC-30.], [Must],
  [NFR-06], [Privasi dan diagnostik], [UI tidak mengungkap rahasia/stack trace. CLI memberi target, kode SQL atau jenis error tanpa password/connection string lengkap. TC-10, TC-51.], [Must],
  [NFR-07], [Integritas], [PK/FK/unique/check constraint dan transaksi mencegah orphan, duplikasi, keputusan ganda, serta kuota berlebih. TC-27, TC-33, TC-54.], [Must],
  [NFR-08], [Persistensi], [Data yang telah dikomit bertahan setelah penutupan aplikasi. Transaksi gagal tidak meninggalkan perubahan sebagian. TC-48, TC-54.], [Must],
  [NFR-09], [Keandalan], [Gagal baca/simpan tidak menutup aplikasi tanpa penanganan; form tetap berisi, status jelas, dan retry tidak menggandakan data. TC-41, TC-42.], [Must],
  [NFR-10], [Kompatibilitas], [Ubuntu desktop x64 wajib sebagai target utama. Dukungan Windows x64 berprioritas Should sebagai target tambahan. Versi OS minimum serta bukti native memerlukan KD-02. TC-45, TC-57.], [Must],
  [NFR-11], [Aksesibilitas], [Label, keyboard, fokus, kontras, dan teks status mengikuti Bagian 10. Verifikasi runtime dan screenshot, bukan pemeriksaan token saja. TC-43, TC-44.], [Must],
  [NFR-12], [Skala tampilan], [Layout pada ukuran minimum dan skala OS yang ditetapkan tidak memotong input/tindakan. Ukuran DIP dibedakan dari piksel fisik. TC-45, KD-02.], [Must],
  [NFR-13], [Preferensi gerak], [Reduced motion menghilangkan gerak yang tidak perlu tanpa menghilangkan teks status atau fungsi. TC-56.], [Must],
  [NFR-14], [Pemeliharaan], [View, ViewModel, model, service, dan akses SQL memiliki tanggung jawab jelas; aturan bisnis dapat diuji tanpa mengendalikan seluruh UI. TC-58 dan review kode.], [Must],
  [NFR-15], [Konfigurasi], [Koneksi berasal dari environment proses; source bersih tidak memerlukan folder agen atau kredensial pribadi. Init/seed melaporkan kegagalan dengan exit code nonnol. TC-46, TC-47, TC-58.], [Must],
  [NFR-16], [Waktu], [Perhitungan batas pendaftaran konsisten dalam UTC; tampilan waktu menyebut WIB dan lolos batas pergantian tanggal. TC-15, TC-52.], [Must],
  [NFR-17], [Logging lanjutan], [Usulan: log teknis terstruktur dengan redaksi data sensitif, lokasi dan retensi yang disepakati. Belum menjadi audit log bisnis persisten. KD-06, TC-51.], [Should],
  [NFR-18], [Benchmark], [Usulan: ukur waktu baca/simpan pada dataset serta perangkat acuan yang disepakati sebelum menjanjikan SLA. Tidak ada angka target yang diasumsikan. KD-01, TC-59.], [Should],
  [NFR-19], [Akses SQL], [Usulan: akun runtime dibatasi sesuai kebutuhan; akun admin SQL dipakai untuk setup lokal yang memerlukannya. Distribusi di luar demo memerlukan KD-05.], [Should],
  [NFR-20], [Backup dan retensi], [Usulan: tetapkan pemilik, retensi, backup, dan prosedur pemulihan bila data dipakai berkelanjutan. Belum ada fitur backup otomatis dalam scope. KD-08.], [Should],
))

#pagebreak()
= Rancangan Data Awal

Rancangan berikut mengikuti konsep dan schema ETC saat penyusunan. Nama tabel dicantumkan agar developer/QA dapat menelusurinya; detail indeks dan migrasi tetap dikelola pada script database. Tidak ada entitas administrasi akademik dari dokumen contoh yang dimasukkan.

== Entitas, atribut inti, dan validasi

#prd-table((28mm, 1fr, 1fr), ([Entitas/tabel], [Atribut inti], [Catatan validasi]), (
  [Mahasiswa / Students], [Id, FullName, Email, NormalizedEmail, PasswordHash, Cohort, Bio, Experience, PortfolioUrl, LinkedInUrl, Photo, IsDemo.], [Nama maksimal 120; email 254 dan unik setelah normalisasi; password input 12 sampai 128; angkatan empat digit tidak melebihi tahun berjalan. Profil awal dapat belum lengkap.],
  [Skill / Skills], [Id dan Name.], [Nama maksimal 80 dan unik. Katalog disiapkan melalui data awal; tidak ada CRUD katalog pada UI rilis awal.],
  [MahasiswaSkill / StudentSkills], [StudentId dan SkillId.], [Pasangan unik; kedua ID harus valid. Minimal satu skill untuk profil lengkap.],
  [Rekrutmen / Recruitments], [Id, OwnerId, Title, Category, ActivityName, Description, DeadlineUtc, StartsOn, EndsOn, Mode, Location, FeeKind, FeeAmount, FeeNote, InfoUrl, ContactName, ContactChannel, Status, PublishedAtUtc, UpdatedAtUtc, IsDemo.], [Judul/kegiatan 200; deskripsi 10.000; lokasi/kontak kanal 500; kontak nama 120; fee note 2.000. Publikasi mengikuti FR-16 dan BR-13.],
  [Posisi / Positions], [Id, RecruitmentId, Name, Capacity.], [Nama maksimal 120; kapasitas integer positif. Accepted count diturunkan dari lamaran diterima, bukan input bebas.],
  [PosisiSkill / PositionSkills], [PositionId dan SkillId.], [Pasangan unik; posisi dan katalog harus ada. Posisi terpublikasi membutuhkan skill.],
  [Lamaran / Applications], [Id, StudentId, RecruitmentId, PositionId, Message, PortfolioUrl, Status, SubmittedAtUtc, DecidedAtUtc.], [Pesan wajib maksimal 2.000; pasangan mahasiswa/rekrutmen unik; posisi harus berasal dari rekrutmen yang sama.],
  [Bookmark / Bookmarks], [StudentId dan RecruitmentId.], [PK gabungan mencegah duplikasi. Hak baca posting tetap diperiksa.],
  [Mentor / Mentors], [Id, Name, StudentNumber, Cohort, Focus, Photo, Experience, Achievements, IsDemo.], [NRP sumber unik jika terisi; menjadi kunci pembaruan import, bukan relasi login. Informasi mengikuti sumber tanpa klaim tambahan.],
  [MentorSkill / MentorSkills], [MentorId dan SkillId.], [Pasangan unik; mendukung pencarian keahlian.],
  [Versi schema / SchemaVersion], [Version.], [Penanda migrasi, bukan data pengguna. Script init harus aman dijalankan kembali.],
))

== Validasi lintas entitas

#prd-table((34mm, 1fr), ([Data], [Ketentuan]), (
  [Tanggal dan waktu], [Tenggat publikasi wajib di masa depan. Akhir kegiatan tidak mendahului awal bila keduanya tersedia. Transaksi memakai UTC; tampilan yang relevan menyebut WIB.],
  [Biaya], [Enum Belum diketahui/Gratis/Berbayar. Gratis bernilai nol; unknown tidak dipaksa nol; paid positif dengan keterangan. Makna label biaya lanjutan memerlukan KD-04.],
  [URL], [Opsional jika tidak diperlukan fitur; maksimal 2.048 karakter, absolut HTTP/HTTPS, host terisi, dan tanpa user-info kredensial. Skema file/javascript atau URL tidak valid ditolak.],
  [Foto profil], [PNG/JPEG yang dapat didekode, maksimal 2 MiB dan 4096 x 4096 piksel. Data tidak cukup divalidasi dari ekstensi nama file.],
  [Foto mentor sumber], [Foto sumber diberikan pengguna; salinan import diperkecil hingga sisi terpanjang 960 piksel sebelum disimpan. Foto asli tidak diubah.],
  [Keunikan dan referensi], [Email normalisasi, satu lamaran per mahasiswa/rekrutmen, pasangan skill/bookmark, serta NRP mentor sumber dijaga sesuai constraint. ID tidak valid tidak boleh menyebabkan perubahan sebagian.],
))

== Relasi utama

#prd-table((43mm, 21mm, 1fr), ([Relasi], [Kardinalitas], [Makna]), (
  [Students -> Recruitments], [1 : N], [Satu mahasiswa dapat memiliki banyak rekrutmen; setiap rekrutmen mempunyai satu pemilik.],
  [Students <-> Skills], [N : M], [Melalui StudentSkills untuk profil mahasiswa.],
  [Recruitments -> Positions], [1 : N], [Satu rekrutmen mempunyai posisi yang dibuka; publikasi membutuhkan minimal satu.],
  [Positions <-> Skills], [N : M], [Melalui PositionSkills untuk persyaratan posisi.],
  [Students -> Applications], [1 : N], [Satu mahasiswa mengirim banyak lamaran pada rekrutmen yang berbeda.],
  [Recruitments -> Applications], [1 : N], [Setiap lamaran terikat pada satu rekrutmen dan satu posisi milik rekrutmen itu.],
  [Positions -> Applications], [1 : N], [FK gabungan menjaga pasangan PositionId/RecruitmentId konsisten.],
  [Students <-> Recruitments], [N : M], [Relasi bookmark melalui Bookmarks, terpisah dari relasi kepemilikan dan lamaran.],
  [Mentors <-> Skills], [N : M], [Melalui MentorSkills. Mentor tidak otomatis menjadi akun Students.],
))

== Asumsi database dan data demonstrasi

Database menggunakan SQL Server. Jalur setup utama memakai instance lokal Docker dan database `ETC_Demo`; CLI seed menolak nama database lain. Integration test memakai database terpisah `ETC_Test` atau `ETC_Test_...` karena fixture dapat membersihkan data test. Konfigurasi koneksi tidak disimpan dalam source dan tidak otomatis dibaca dari berkas `.env`.

Dataset seed saat penyusunan berisi *3 akun demo, 8 rekrutmen, 7 lamaran, dan 15 mentor sumber beserta foto*. Angka tersebut berasal dari seeder project, bukan target pengguna, kapasitas produk, atau batas isi database. Ketiga akun demo memakai password bawaan `Etc-Demo!2026` jika dibuat tanpa override. Data manual dan password akun lama dipertahankan saat seed ulang.

Penghapusan mahasiswa dan kebijakan retensi belum menjadi fitur produk. Jangan menambahkan cascade delete atau prosedur reset ke alur pengguna tanpa keputusan terpisah. Kebijakan backup/penggunaan data berkelanjutan mengikuti KD-08.

= Rekomendasi Arsitektur Teknis

== Susunan dan tanggung jawab

Arsitektur mengikuti implementasi desktop yang ada: satu project aplikasi, folder per fitur, MVVM, service, dan query ADO.NET. Tidak diwajibkan menambahkan generic repository, microservice, atau backend baru untuk menyelesaikan demonstrasi.

#prd-table((29mm, 1fr, 48mm), ([Bagian], [Tanggung jawab], [Lokasi/contoh]), (
  [View], [AXAML untuk layout, binding, dan interaksi visual; code-behind terbatas pada perilaku UI seperti fokus/menu.], [`Features/*/*View.axaml`, `Shared/ShellView`],
  [ViewModel], [State halaman, command, validasi tampilan, busy/error, dan pemanggilan service.], [`ProfileViewModel`, `SearchViewModel`, `ApplicantsViewModel`],
  [Model], [Record input/proyeksi data dan enum status; tidak menyimpan state visual global.], [`ProfileModels`, `RecruitmentModels`, `ApplicationModels`],
  [Service], [Sesi, hak akses, validasi bisnis, transaksi, query, dan pemetaan data.], [`AccountService`, `RecruitmentService`, `ApplicationService`],
  [Infrastructure], [Koneksi, parameter SQL, pemetaan hasil, dan initializer resource SQL.], [`AppSettings`, `SqlConnectionFactory`, `Db`, `DatabaseInitializer`],
  [Shared], [State sesi, navigasi, error domain, guard, URL launcher, dan adaptor motion.], [`SessionState`, `ShellViewModel`, `PageViewModel`, `Guard`],
  [Composition root], [Menyusun dependensi dan navigasi, menyediakan dialog konfirmasi serta launcher eksternal.], [`CompositionRoot.cs`, `Program.cs`, `App.axaml`],
  [Data awal dan aset], [Seed demo/mentor; schema; foto, font dan ikon yang dibundel.], [`Demo`, `database`, `Assets`],
))

Alur utama: *View → ViewModel/command → service → koneksi dan query ADO.NET → SQL Server → proyeksi data → state ViewModel → binding UI*. Aturan akses tidak dipindahkan ke warna, tombol, atau converter.

== Stack dan konfigurasi

#prd-table((36mm, 1fr), ([Komponen], [Baseline saat penyusunan]), (
  [Runtime dan SDK], [.NET 10 (`net10.0`); SDK diminta `10.0.112` dengan roll-forward latestFeature.],
  [UI dan MVVM], [Avalonia 12.1.3; CommunityToolkit.Mvvm 8.4.2; AXAML compiled bindings.],
  [Database], [SQL Server; contoh setup memakai SQL Server 2022 Developer pada Docker x64.],
  [Akses data], [ADO.NET, Microsoft.Data.SqlClient 7.1.1, parameter query, async, dan transaksi SQL.],
  [Password], [Microsoft.Extensions.Identity.Core PasswordHasher; password akun tidak disimpan sebagai teks biasa.],
  [Konfigurasi SQL], [`MSSQL_SA_PASSWORD` menetapkan kredensial SQL saat init volume baru. `etcSqlPassword` hanya variabel shell sementara yang diisi otomatis pada jalur Docker. `ETC_CONNECTION_STRING` adalah konfigurasi yang dibaca aplikasi.],
  [Konfigurasi demo], [`ETC_DEMO_PASSWORD` bersifat opsional untuk override; jika tidak ada, seeder memakai password bawaan. Bukan password SQL dan tidak mengganti akun yang sudah ada.],
  [Form mentoring], [URL default dari sumber mentor; `ETC_MENTOR_FORM_URL` dapat menggantinya. Nilai kosong berarti tindakan tidak tersedia.],
))

Versi paket di atas adalah snapshot codebase, bukan janji versi terbaru. Dependensi dan lisensi harus tetap diperiksa ketika diperbarui. Akses SQL langsung sesuai konteks demonstrasi lokal; penggunaan oleh pengguna publik atau jaringan di luar lingkungan tepercaya memerlukan rancangan kredensial/backend tersendiri melalui KD-05.

= Alur Pengguna Utama

== Alur akun dan persiapan profil

1. Mahasiswa membuka onboarding, memilih Buat akun, dan mengisi nama/email/password.
2. Sistem memvalidasi dan menyimpan akun; mahasiswa masuk melalui login.
3. Mahasiswa melengkapi angkatan dan skill melalui Profil saya; foto/tautan bersifat opsional.
4. Sistem mengizinkan pembuatan rekrutmen/pelamaran setelah profil lengkap.
5. Jika validasi atau koneksi gagal, isian tetap tersedia dan sistem memberi langkah perbaikan.

== Alur merekrut dan menyeleksi

1. Pembuat membuka Rekrutmen saya dan memilih Buat rekrutmen.
2. Pembuat menyimpan draft, mengisi posisi/skill/kuota, informasi kegiatan, biaya/kontak, dan tenggat.
3. Pembuat mengonfirmasi publikasi; sistem memvalidasi ulang sebelum menyimpan status Dibuka.
4. Pembuat membuka daftar pelamar per posisi, membaca pesan dan profil terbaru, lalu mengonfirmasi Terima/Tolak.
5. Sistem mengunci konteks rekrutmen, memeriksa status/kuota, dan mengomit keputusan secara atomik.
6. Pembuat dapat menutup rekrutmen tanpa menghapus riwayat. Pengajuan lama masih dapat diputuskan sesuai BR-10.

== Alur mencari dan melamar

1. Mahasiswa membuka Cari rekrutmen; filter awal menampilkan posisi yang masih menerima lamaran.
2. Mahasiswa mengubah pencarian/filter, membuka detail, dan menyimpan bookmark jika diperlukan.
3. Mahasiswa memilih posisi, mengisi pesan, dan mengirim lamaran.
4. Sistem memeriksa profil, kepemilikan, duplikasi, hubungan posisi, tenggat, status, dan kuota saat transaksi.
5. Mahasiswa membaca Menunggu pada Lamaran saya; pengajuan dapat dibatalkan sebelum keputusan.
6. Setelah pemuatan ulang, mahasiswa membaca Diterima/Ditolak. Kegiatan tim selanjutnya dilakukan di luar ETC.

== Alur mentor

1. Mahasiswa membuka Mentor, mencari nama/keahlian atau memilih tombol skill.
2. Mahasiswa membuka detail dan menilai pengalaman dari data sumber.
3. Mahasiswa memilih Butuh mentor?; URL valid dibuka di browser.
4. Jika URL tidak tersedia/browser gagal, ETC menampilkan alasan. Pengisian dan respons formulir berlangsung di luar ETC.

== Alur setup dan penggunaan lintas komputer

1. Penyiap menyalin source yang diperlukan dan memasang SDK, Docker/SQL, serta dependensi desktop.
2. Penyiap membuat SQL dengan password pilihannya dan membuat database `ETC_Demo`.
3. Pada jalur Docker baru, konfigurasi koneksi mengambil password container secara otomatis ke variabel sementara, lalu membentuk `ETC_CONNECTION_STRING`.
4. Penyiap menjalankan restore, build, init schema, dan seed secara berurutan; tahap gagal menghentikan kelanjutan setup.
5. Seed memakai password demo bawaan jika tidak ada override; mahasiswa dapat login dengan akun yang dibuat.
6. Pada terminal baru, koneksi diatur kembali sebelum menjalankan aplikasi. Data berada pada SQL komputer/instance yang dituju, bukan otomatis ikut source.

= Rencana Implementasi Bertahap

Rencana ini menjadi urutan pembangunan atau pelengkapan kandidat rilis demonstrasi. Banyak modul telah memiliki implementasi; tabel tidak menyatakan status penyelesaiannya. Durasi, penanggung jawab bernama, dan tenggat akademik mengikuti KD-03.

#prd-table((22mm, 1fr, 1fr, 26mm), ([Tahap], [Pekerjaan], [Keluaran dan syarat keluar], [Ketergantungan]), (
  [Fase 1: baseline], [Selaraskan scope, keputusan terbaru, aturan status/akses, serta pertanyaan terbuka.], [PRD ditinjau pemilik produk; blocker rilis diidentifikasi; referensi FR/US/TC konsisten.], [KD-02, KD-03],
  [Fase 2: fondasi], [Pastikan stack, composition root, schema, konfigurasi, seed idempotent, dan setup bersih.], [Build serta init/seed berhasil pada SQL terpisah; tidak memerlukan folder agen/kredensial pembuat.], [Fase 1],
  [Fase 3: akun/profil], [Lengkapi akun, sesi, otorisasi, validasi profil, foto, dan proyeksi publik.], [US-01 sampai US-05 lulus; pengujian negatif akses tersedia.], [Fase 2],
  [Fase 4: rekrutmen], [Pencarian, detail, bookmark, draft, posisi, publikasi, pembaruan, tutup/hapus.], [US-07 sampai US-13 lulus; draft tidak bocor dan batas edit ditegakkan.], [Fase 3],
  [Fase 5: seleksi], [Pengajuan, pending/final, pembatalan, peninjauan, kuota atomik, dan dashboard dinamis.], [US-06 dan US-14 sampai US-18 lulus, termasuk race pada slot terakhir.], [Fase 4],
  [Fase 6: mentor/UI], [Direktori sumber, foto, detail, formulir, state UI, keyboard, kontras, dan layout.], [US-19 sampai US-23 lulus; review visual sesuai desain dan tanpa fakta buatan.], [Fase 3; data mentor],
  [Fase 7: verifikasi], [Regresi, pengujian lintas komputer, perbaikan, README, serta paket demonstrasi bila diperlukan.], [Kriteria Must yang disepakati lulus; bukti dicatat; batas native Windows/benchmark tidak disamarkan.], [Seluruh fase; keputusan blocker],
))

Penambahan fitur di luar scope memerlukan pembaruan PRD, prioritas, kriteria penerimaan, dan pengujian sebelum dimasukkan ke rilis. Rencana tidak menetapkan tanggal atau estimasi yang belum disetujui.

#pagebreak()
= Strategi Pengujian

== Tingkat pengujian dan persiapan

#prd-table((32mm, 1fr), ([Tingkat], [Cakupan dan lingkungan]), (
  [Unit], [Aturan status/tenggat, guard, validasi foto/URL, state sesi, command dan alur ViewModel yang dapat diisolasi.],
  [Integrasi SQL], [Query nyata, constraint, kepemilikan, transaksi, persistensi, seed idempotent, dan konkurensi. Gunakan database test terpisah; jangan membersihkan database aplikasi.],
  [UI headless], [Binding, navigasi, label, keyboard, state, warna runtime, serta render pada ukuran yang ditetapkan. Tidak menggantikan pengujian native OS.],
  [Manual native], [Jalankan desktop pada Ubuntu utama; cek input/fokus/dialog, file picker foto, browser, resize, dan skala. Windows tambahan diverifikasi bila dijanjikan sebagai target rilis.],
  [Setup bersih], [Salinan source tanpa bin/obj/folder agen dan SQL baru terpisah. Buktikan urutan README sampai login; tidak bergantung pada environment komputer pembuat.],
))

Siapkan akun A dan B sebagai mahasiswa, posting dengan beberapa posisi/status, lamaran untuk seluruh status, slot terakhir untuk uji race, sumber mentor dengan foto, serta skenario koneksi/form eksternal gagal. Data fixture dinyatakan sebagai demonstrasi; tidak mencampur data test dengan data manual pengguna.

== Skenario minimum dan hasil yang diharapkan

Tabel berikut adalah rencana penerimaan, bukan daftar tes yang diklaim telah dijalankan. Pelaksana harus mencatat versi source, lingkungan, waktu, status aktual, dan bukti untuk setiap TC.

#prd-table((15mm, 30mm, 1fr, 1fr), ([ID], [Area/kebutuhan], [Skenario minimum], [Hasil yang diharapkan]), (
  [TC-01], [Onboarding; FR-01], [Buka aplikasi belum login; gunakan Masuk dan Buat akun.], [Halaman akun terbuka; tidak ada menu privat yang dapat digunakan tanpa sesi.],
  [TC-02], [Registrasi; FR-02], [Daftarkan email, lalu ulangi dengan variasi huruf besar/kecil dan spasi tepi.], [Satu akun terbentuk; email normalisasi yang sama ditolak tanpa duplikasi.],
  [TC-03], [Password; FR-02, NFR-03], [Password di bawah batas, tepat batas, melebihi batas; periksa nilai tersimpan.], [Batas 12 sampai 128 ditegakkan saat pendaftaran; nilai tersimpan berupa hash.],
  [TC-04], [Login; FR-03], [Masuk dengan kredensial valid.], [Sesi dan dashboard sesuai identitas akun.],
  [TC-05], [Login gagal; FR-03], [Email tidak terdaftar atau password salah.], [Pesan aman; tidak ada sesi atau akses data privat.],
  [TC-06], [Otorisasi; FR-05], [Akses tanpa login; akun B meminta edit/seleksi pada milik A melalui service.], [Operasi ditolak dan tidak mengubah data.],
  [TC-07], [Logout; FR-04], [Pilih Keluar lalu Batal/Escape; ulangi dan setujui.], [Pembatalan mempertahankan sesi; persetujuan menghapus sesi dan konteks akun lama.],
  [TC-08], [Profil; FR-06], [Simpan nama, angkatan dan skill valid; tambahkan foto/tautan yang sah.], [Profil sendiri tersimpan dan dapat dibaca kembali.],
  [TC-09], [Kelengkapan; FR-07], [Akun baru belum melengkapi angkatan atau skill lalu membuat posting/melamar.], [Aksi ditolak dengan arahan melengkapi profil; tidak ada perubahan bisnis.],
  [TC-10], [Privasi profil; FR-08], [A membaca profil B.], [Informasi publik terlihat; email login/hash tidak dikirim pada proyeksi publik; edit tidak tersedia untuk A.],
  [TC-11], [Dashboard; FR-09], [Bandingkan statistik terhadap query akun A/B, termasuk akun tanpa aktivitas.], [Nilai benar per akun; nol dan empty state jujur; pintasan menuju daftar yang sesuai.],
  [TC-12], [Pencarian; FR-10], [Ubah kata kunci, kategori, skill/posisi dan OnlyAvailable; gunakan karakter wildcard sebagai input.], [Hasil mengikuti filter, wildcard input diperlakukan aman, dan draft akun lain tidak muncul.],
  [TC-13], [Pagination; FR-11], [Dataset lebih dari 12 item; pindah halaman, ubah filter, buka detail dan Kembali.], [Tidak ada item hilang/duplikat akibat urutan; filter kembali halaman awal; konteks daftar dipulihkan.],
  [TC-14], [Detail; FR-12], [Buka posting publik, draft sendiri, dan ID draft akun lain.], [Detail lengkap sesuai akses; draft akun lain tidak terbaca.],
  [TC-15], [Ketersediaan; FR-36, FR-37], [Periksa sebelum, tepat pada, dan setelah tenggat; posisi penuh dan posting ditutup.], [Batas waktu konsisten; alasan terlihat; tidak ada pengajuan baru pada kondisi tertutup.],
  [TC-16], [Bookmark; FR-13], [Simpan posting yang sama berulang lalu hapus bookmark.], [Pasangan unik; hitungan sesuai; posting tetap ada setelah bookmark dihapus.],
  [TC-17], [Bookmark riwayat; FR-13], [Simpan posting lalu pemilik menutupnya.], [Posting tetap dapat dibuka dari bookmark; aksi melamar tidak tersedia.],
  [TC-18], [Draft; FR-14], [Simpan draft belum lengkap dengan profil lengkap; login sebagai akun lain.], [Draft tersimpan untuk pemilik dan tidak tampak sebagai posting publik.],
  [TC-19], [Posisi; FR-15], [Kuota nol/negatif, nama posisi kosong, dan posisi publik tanpa skill.], [Nilai tidak valid ditolak; tidak ada posisi terpublikasi yang melanggar ketentuan.],
  [TC-20], [Publikasi; FR-16], [Lengkapi draft lalu konfirmasi publikasi.], [Status menjadi Dibuka, waktu publikasi tercatat, dan muncul sesuai filter publik.],
  [TC-21], [Publikasi gagal; FR-16], [Tenggat lampau, kontak kosong, mode belum diisi, atau offline tanpa lokasi.], [Publikasi ditolak; draft/isian tidak hilang.],
  [TC-22], [Kunci posisi; FR-17], [Sesudah lamaran masuk, ubah nama posisi, skill atau kuota; ubah kontak terpisah.], [Perubahan posisi ditolak; kontak valid tetap dapat diperbarui dengan timestamp.],
  [TC-23], [Tenggat edit; FR-17], [Ubah tenggat saat tersedia, lalu ulangi pada penuh, lewat tenggat, atau ditutup.], [Hanya kondisi BR-09 diizinkan; pengajuan lama dipertahankan.],
  [TC-24], [Tutup; FR-18], [Batalkan dialog penutupan lalu konfirmasi; coba mengirim lamaran baru.], [Batal tidak menulis; konfirmasi menutup; riwayat tetap ada dan lamaran baru ditolak.],
  [TC-25], [Hapus draft; FR-19], [Hapus draft sendiri; coba milik akun lain dan posting terpublikasi.], [Hanya draft sendiri terhapus setelah konfirmasi; operasi lain ditolak.],
  [TC-26], [Lamaran; FR-20], [B berprofil lengkap melamar posisi A yang tersedia dengan pesan valid.], [Satu lamaran Menunggu terbentuk pada posisi/rekrutmen yang benar.],
  [TC-27], [Larangan; FR-21], [Lamar posting sendiri; ulangi pengajuan sesudah pending, ditolak, atau dibatalkan.], [Semua pengajuan yang melanggar BR-04/BR-05 ditolak tanpa duplikasi.],
  [TC-28], [Race pengajuan; FR-20, FR-21], [Buka detail lalu ubah ketersediaan di transaksi lain sebelum submit; coba PositionId dari posting lain.], [Validasi transaksi menolak kondisi usang/posisi salah tanpa pengajuan sebagian.],
  [TC-29], [Pembatalan; FR-22], [Batalkan pending sendiri, coba milik orang lain, dan status final.], [Hanya pending sendiri dapat menjadi Dibatalkan; batal dialog tidak menulis.],
  [TC-30], [Pelamar; FR-23], [Pemilik membaca daftar per posisi dan nonpemilik meminta daftar melalui service.], [Pemilik mendapat data benar; nonpemilik ditolak, termasuk akses pesan/profil privat lamaran.],
  [TC-31], [Seleksi; FR-24], [Terima dan tolak dua lamaran pending; batalkan salah satu dialog terlebih dahulu.], [Keputusan tersimpan hanya setelah konfirmasi; hasil terbaca oleh pelamar setelah reload.],
  [TC-32], [Status final; FR-24], [Ulangi keputusan pada lamaran final; putuskan pending lama setelah posting ditutup.], [Final tidak berubah; pending lama masih dapat diputuskan dengan pemeriksaan kuota.],
  [TC-33], [Konkurensi; FR-25], [Dua penerimaan bersamaan ketika tersisa satu slot.], [Paling banyak satu berhasil; accepted count tidak melampaui kapasitas; pihak lain mendapat konflik/alasan.],
  [TC-34], [Riwayat; FR-26], [Pelamar membaca seluruh status pada beberapa halaman, lalu berpindah akun.], [Riwayat terurut dan privat; hanya pending memiliki pembatalan; state akun lama dibersihkan.],
  [TC-35], [Mentor; FR-27], [Baca seluruh mentor sumber pada lebar berbeda, termasuk nama/skill panjang.], [Foto sumber terbaca, kartu seragam pada lebar sama, teks dan tombol tidak terpotong.],
  [TC-36], [Cari mentor; FR-28], [Cari nama/skill, pilih skill melalui keyboard, reset, tanpa hasil, pindah halaman dan Kembali.], [Hasil/pagination benar; konteks kembali terjaga; empty state dan reset tersedia; tidak ada Muat ulang tambahan.],
  [TC-37], [Detail mentor; FR-29], [Buka detail sumber dengan dan tanpa pengalaman/portofolio.], [Data sumber tepat; nilai kosong dijelaskan; tidak ada rating/status online buatan.],
  [TC-38], [Form/browser; FR-30], [URL form valid, kosong, tidak valid, serta browser gagal membuka.], [Browser dibuka hanya untuk URL sah; kondisi lain diberi alasan; tidak ada klaim respons tersimpan.],
  [TC-39], [Validasi form; FR-31], [Uji kolom wajib, batas panjang, enum, tanggal terbalik, paid nol/tanpa keterangan, dan URL non-HTTP/HTTPS.], [Service menolak input invalid; UI memberi pesan yang dapat diperbaiki dan mempertahankan nilai.],
  [TC-40], [Foto; FR-31], [PNG/JPEG sah; file rusak, tipe lain, lebih dari 2 MiB, dimensi lebih dari batas.], [Hanya foto yang lolos decode/format/ukuran diterima; pesan tidak memuat rahasia teknis.],
  [TC-41], [Koneksi gagal; FR-32], [Putuskan atau lambatkan SQL saat membaca/menyimpan.], [UI tetap merespons; status loading/error jelas; form tidak dikosongkan; tidak ada sukses palsu.],
  [TC-42], [Retry dan refresh; FR-32], [Gagal baca lalu coba lagi; gagal simpan lalu ulangi; picu submit ganda.], [Pemulihan bekerja tanpa duplikasi; data lama ditandai dengan waktu muat, bukan dianggap terbaru.],
  [TC-43], [Keyboard; FR-33], [Selesaikan alur login, navbar, filter, form, dialog, dan menu ringkas dengan Tab/Enter/Escape.], [Fokus terlihat dan tidak terjebak; label aksesibel tersedia; Batal/Escape aman.],
  [TC-44], [Tema/kontras; FR-38], [Jalankan saat tema OS gelap; periksa teks/ikon profil dan keluar, hover/pressed/focus/disabled serta permukaan daftar.], [Aplikasi tetap terang, tanpa toggle; navbar transparan berikon; kontras memenuhi Bagian 10.],
  [TC-45], [Layout; FR-33, NFR-12], [Render pada tiga ukuran DIP yang ditetapkan; cek native 100%/150% pada OS sasaran.], [Tidak ada tindakan/teks penting terpotong atau scroll horizontal seluruh halaman; bukti native dicatat terpisah.],
  [TC-46], [Setup default; FR-34], [SQL baru dan environment koneksi valid; jalankan restore/build/init/seed tanpa ETC_DEMO_PASSWORD.], [Schema dan seed berhasil; tiga akun demo baru dapat login dengan Etc-Demo!2026.],
  [TC-47], [Guard dan override; FR-34], [Seed ke database selain ETC_Demo; override invalid; override valid pada akun baru; seed ulang dengan nilai berbeda.], [Target salah/override invalid ditolak; nilai valid dipakai saat pembuatan; password akun lama tetap.],
  [TC-48], [Persistensi; FR-35], [Simpan profil/posting/lamaran lalu tutup dan buka kembali aplikasi dengan SQL sama.], [Data dikomit tetap tersedia; sesi login tidak otomatis dipulihkan.],
  [TC-49], [Konfirmasi; FR-39], [Batal/setujui publikasi, seleksi, tutup, hapus draft, batal lamaran, dan logout.], [Judul/aksi jelas; pembatalan tidak menulis; hasil sukses hanya sesudah komit/akhir sesi yang benar.],
  [TC-50], [Keamanan; NFR-04, NFR-05], [Masukkan karakter SQL pada pencarian/form dan panggil service dengan ID milik akun lain.], [Input tidak menjadi perintah SQL; akses ditolak tanpa kebocoran atau perubahan data.],
  [TC-51], [Diagnostik; NFR-06, NFR-17], [Koneksi kosong/malformed, password SQL salah, database tidak dapat dibuka; periksa UI/CLI.], [CLI membedakan konfigurasi/SQL error tanpa password; UI aman. Logging persisten tetap usulan KD-06.],
  [TC-52], [Zona waktu; NFR-16], [Waktu UTC melewati tengah malam WIB; kondisi tepat tenggat.], [Tanggal/jam WIB benar dan logika tenggat UTC konsisten.],
  [TC-53], [Sesi async; FR-05], [Logout atau ganti akun ketika operasi baca/login akun lama masih berlangsung.], [Hasil terlambat tidak memulihkan sesi atau data privat akun sebelumnya.],
  [TC-54], [Rollback; NFR-07, NFR-08], [Gunakan skill/FK invalid ketika memperbarui data multi-tabel.], [Seluruh transaksi gagal tanpa penghapusan/penambahan sebagian; constraint tetap berlaku.],
  [TC-55], [Import sumber; FR-35], [Ubah nama/foto mentor dengan NRP sama, seed ulang, dan pertahankan mentor tambahan/data manual.], [Profil sumber diperbarui tanpa duplikasi; entitas lain dipertahankan; foto sumber asli tidak dimodifikasi.],
  [TC-56], [Motion; NFR-13], [Aktifkan reduced motion lalu lakukan operasi yang memakai feedback/progres.], [Gerak dikurangi; label, loading, navigasi, dan hasil tetap dapat digunakan.],
  [TC-57], [Kompatibilitas; NFR-10], [Jalankan kandidat pada versi Ubuntu/Windows yang disepakati, termasuk file picker dan browser.], [Hasil native dicatat per OS. Sebelum pengujian, dukungan tambahan tidak dinyatakan terverifikasi.],
  [TC-58], [Source bersih; NFR-14, NFR-15], [Salin file runtime/build tanpa folder agen dan hasil build lama; tinjau pembagian lapisan.], [Restore/build serta alur inti berjalan tanpa konfigurasi pribadi; aturan bisnis tidak hanya berada pada code-behind.],
  [TC-59], [Benchmark; NFR-02, NFR-18], [Setelah KD-01 disepakati, ukur baca/simpan pada dataset dan perangkat acuan.], [Hasil, metode, ukuran data, dan ambang dicatat; tidak memberi status lulus SLA sebelum target ditetapkan.],
  [TC-60], [Pemicu notifikasi; FR-39], [Jalankan setiap aktivitas pada @notifikasi-sukses dengan data valid; periksa ketika operasi masih berjalan dan setelah penyimpanan selesai.], [Belum ada pesan sukses selama proses. Setelah data berhasil tersimpan, banner Berhasil muncul dengan pesan sesuai tindakan.],
  [TC-61], [Persistensi dan penutupan pesan; FR-39], [Setelah penyimpanan, gulir halaman, pindah dari form ke detail, lalu tutup banner dengan mouse dan keyboard. Uji navigasi Kembali serta logout.], [Pesan tetap terlihat saat scroll dan perpindahan halaman dalam sesi yang sama. Tutup menghilangkan pesan tanpa mengambil fokus secara otomatis; Kembali tidak memunculkannya lagi. Logout membersihkan pesan.],
  [TC-62], [Notifikasi pada kegagalan/pembatalan; FR-39], [Gagalkan penyimpanan melalui input tidak valid atau koneksi terputus; batalkan dialog konfirmasi. Secara terpisah, gagalkan pemuatan ulang setelah penyimpanan berhasil lalu pilih Coba lagi.], [Gagal simpan dan pembatalan tidak menghasilkan pesan sukses untuk tindakan tersebut. Jika hanya pemuatan ulang yang gagal, sukses simpan dan error baca dibedakan; retry hanya mengulang pembacaan tanpa write kedua.],
))

== Pencatatan hasil dan regresi

Untuk setiap pengujian, catat ID TC, commit atau identitas source yang diuji, OS/SDK/database, prasyarat, langkah aktual, hasil, status, serta tautan bukti bila ada. Penggunaan identitas source tidak mensyaratkan perintah Git tertentu. Pisahkan hasil unit, integrasi, headless, dan native. Perbaikan cacat harus diuji ulang pada skenario yang gagal dan alur terkait; jangan menganggap build berhasil berarti seluruh acceptance criteria lulus.

= Risiko dan Mitigasi

#prd-table((14mm, 31mm, 1fr, 1fr), ([ID], [Risiko], [Dampak], [Mitigasi]), (
  [R-01], [Kredensial SQL berbeda], [Init/login/seed gagal meskipun container Up atau database sudah ada.], [Tes login SQL, tampilkan target/kode error aman, sinkronkan environment terminal dengan kredensial yang valid.],
  [R-02], [Volume SQL lama], [Password environment container baru tidak cocok dengan akun SQL tersimpan.], [Dokumentasikan bahwa volume mempertahankan akun; pemulihan setup kosong memakai volume baru dan menjaga data lama.],
  [R-03], [Kuota diperebutkan], [Dua keputusan dapat melebihi kapasitas jika pemeriksaan terpisah.], [Transaksi, penguncian, pemeriksaan ulang, dan TC-33.],
  [R-04], [Otorisasi UI saja], [Data privat atau perubahan milik akun lain dapat diakses dari pemanggilan service.], [Periksa sesi/kepemilikan di service dan constraint; pengujian negatif akses.],
  [R-05], [State async usang], [Data/sesi akun lama muncul setelah logout atau pergantian akun.], [Cancellation/revisi sesi, deactivation halaman, dan TC-53.],
  [R-06], [Sumber mentor berubah], [Nama/foto salah, aset tidak ditemukan, atau duplikasi profil.], [NRP sebagai kunci import, validasi path/case, verifikasi foto sumber, dan KD-07.],
  [R-07], [Form/browser eksternal], [Tautan tidak tersedia atau browser gagal; pengguna mengira ETC menyimpan pengajuan.], [Validasi URL, alasan disabled/error, dan penjelasan proses eksternal.],
  [R-08], [Perbedaan OS/skala], [Font, dialog native, fokus, atau layout berbeda pada komputer demonstrasi.], [Aset lokal, uji ukuran DIP dan native OS, serta keputusan versi minimum KD-02.],
  [R-09], [Perluasan scope], [Chat, pembayaran, atau admin menggeser fokus demonstrasi.], [Gunakan batas scope dan pemetaan FR/US/TC; perubahan memerlukan persetujuan rilis baru.],
  [R-10], [Penggunaan di luar demo], [Akses SQL langsung dan akun contoh digunakan tanpa rancangan distribusi yang sesuai.], [Batasi klaim pada demonstrasi; tentukan arsitektur/kredensial baru melalui KD-05 sebelum penggunaan publik.],
  [R-11], [Data test tercampur], [Reset fixture menghapus data aplikasi/manual.], [Gunakan database ETC_Test terpisah dengan guard; verifikasi nama sebelum operasi pembersihan.],
  [R-12], [Target performa diasumsikan], [Rilis dinilai terhadap SLA atau kapasitas yang tidak pernah disepakati.], [Jangan mengarang angka; tetapkan dataset dan target melalui KD-01 sebelum benchmark.],
))

= Asumsi dan Keputusan yang Masih Dibutuhkan

== Dasar dan penyelarasan informasi

#prd-table((28mm, 1fr), ([Sumber/status], [Penetapan pada PRD]), (
  [Permintaan pengguna], [Mahasiswa merekrut mahasiswa; aplikasi desktop C\#; demonstrasi tugas kuliah; tema terang; mentor dari sumber pengguna; navbar teks/ikon profil biru dan keluar merah; setup demo memakai password bawaan seeder.],
  [OUTLINE.md], [Dipakai untuk konteks dan alur. Pernyataan yang masih berupa usulan tidak otomatis dianggap persetujuan baru.],
  [Keputusan lebih baru], [Ubuntu menjadi target demonstrasi utama; .NET 10/Avalonia, desain, foto mentor, serta CLI mengikuti keputusan/codebase terkini, menggantikan usulan lama Windows-only atau visual belum ditetapkan.],
  [Asumsi A-01], [Demonstrasi berlangsung pada lingkungan SQL yang dikelola pihak penyiap; source yang dibagikan bukan distribusi layanan publik.],
  [Asumsi A-02], [Internet tersedia ketika membuka Google Form/portofolio dan mengunduh dependensi awal. Kegagalan internet ditangani sebagai kegagalan layanan eksternal.],
  [Asumsi A-03], [Data mentor yang diberikan menjadi sumber identitas, foto, dan pengalaman yang ditampilkan; pemilik proyek menentukan kelayakan penggunaan dan pembaruannya.],
  [PDF contoh], [Hanya menjadi referensi urutan, tabel, sampul, gaya formal, dan kedalaman pembahasan. Produk, domain, fitur, angka, serta asumsi contoh tidak diadopsi.],
))

== Keputusan terbuka

#prd-table((16mm, 1fr, 1fr, 28mm), ([ID], [Keputusan yang dibutuhkan], [Dampak / sikap sementara], [Pihak terkait]), (
  [KD-01], [Dataset benchmark, perangkat acuan, ukuran data, dan target waktu respons.], [Belum ada SLA numerik. Pagination/async tetap Must; benchmark Should menunggu target.], [Pemilik produk, developer, QA],
  [KD-02], [Versi Ubuntu/Windows minimum dan lingkungan native skala yang menjadi janji rilis.], [Ubuntu x64 prioritas. Jangan menyatakan Windows atau seluruh skala terverifikasi sebelum bukti tersedia.], [Pemilik produk, QA],
  [KD-03], [Rubrik kuliah, tenggat, identitas pengesah, dan ruang lingkup kandidat yang dikumpulkan.], [Tidak menetapkan tanggal, anggaran, nilai akademik, atau nama penanggung jawab yang tidak diberikan.], [Pemilik produk, dosen/penguji],
  [KD-04], [Istilah biaya yang paling tepat dan kategori peruntukan yang ingin dibakukan.], [Saat ini peruntukan dijelaskan pada keterangan biaya. Tidak menganggap seluruh fee sebagai upah atau biaya pendaftaran.], [Pemilik produk],
  [KD-05], [Apakah demonstrasi memakai SQL per komputer, SQL bersama, atau akan menjadi produk publik.], [Panduan utama memakai database lokal tiap komputer. SQL bersama memerlukan kebijakan akses; produk publik memerlukan rancangan baru.], [Pemilik produk, developer],
  [KD-06], [Lokasi, format, akses, dan retensi log persisten.], [CLI aman dan error UI tetap Must. Audit log bisnis persisten tidak dinyatakan telah tersedia.], [Developer, QA, pemilik produk],
  [KD-07], [Pemilik pembaruan mentor, dasar penggunaan foto/profil, dan pemeliharaan Google Form.], [Sumber pengguna dipertahankan; tidak menambahkan klaim verifikasi atau ketersediaan mentor.], [Pemilik produk/pengelola data],
  [KD-08], [Backup, retensi, dan pemulihan jika data dipakai setelah demonstrasi.], [Tidak ada fitur backup otomatis atau penghapusan akun dalam scope awal.], [Pemilik produk, penyiap database],
  [KD-09], [Batas penemuan profil publik di luar konteks pembuat/pelamar.], [Implementasi membaca profil publik untuk sesi mahasiswa; pembatasan akses hanya pada konteks tertentu belum dinyatakan sebagai kemampuan yang terbukti.], [Pemilik produk, developer],
  [KD-10], [Collation dan kebijakan pencarian huruf besar/kecil/aksen lintas instance SQL.], [Pencarian judul/nama/skill tetap wajib; perilaku lintas collation tidak diasumsikan identik tanpa pengaturan dan uji.], [Developer, QA],
))

Keputusan yang mengubah kebutuhan Must atau cakupan rilis harus dicatat bersama tanggal, pihak yang menyetujui, FR/US/TC terdampak, dan versi dokumen. Persyaratan yang belum disepakati tidak boleh diberi status implementasi/tes lulus hanya karena tercantum dalam PRD.

= Definisi Selesai (Definition of Done)

== Checklist kandidat rilis

Status awal checklist ini adalah *Belum dinilai*. Checklist merupakan syarat penerimaan, bukan sertifikat bahwa produk sudah selesai.

#prd-table((15mm, 1fr, 35mm), ([ID], [Syarat selesai], [Bukti yang dibutuhkan]), (
  [DOD-01], [Ruang lingkup rilis dan keputusan yang memblokir target demonstrasi telah disahkan.], [Versi PRD dan catatan persetujuan.],
  [DOD-02], [Seluruh FR Must yang disepakati memiliki implementasi, story/AC, dan skenario penerimaan yang lulus.], [Hasil TC dan bukti per alur.],
  [DOD-03], [Akun, sesi, privasi, validasi, serta pengujian negatif hak akses lulus.], [TC-02 sampai TC-10, TC-50, TC-53.],
  [DOD-04], [Status rekrutmen/lamaran, duplikasi, tenggat, kunci posisi, dan kuota atomik memenuhi aturan.], [TC-15, TC-19 sampai TC-34, TC-54.],
  [DOD-05], [Dashboard/kuota/status berasal dari data dan data tersimpan bertahan setelah restart.], [TC-11, TC-33, TC-48.],
  [DOD-06], [Mentor, foto, pencarian, dan tautan mengikuti sumber serta batas proses eksternal.], [TC-35 sampai TC-38, TC-55.],
  [DOD-07], [Loading, empty/error, retry, konfirmasi, isian gagal simpan, serta kemunculan dan penutupan notifikasi sukses diuji.], [TC-07, TC-41, TC-42, TC-49, TC-60 sampai TC-62.],
  [DOD-08], [Tema terang, ikon/label, kontras, fokus, reflow, serta preferensi gerak diverifikasi.], [TC-43 sampai TC-45, TC-56; screenshot dan bukti native terkait.],
  [DOD-09], [Source bersih, schema, aset/lisensi, README, serta init/seed/run diuji pada lingkungan tujuan.], [TC-46, TC-47, TC-58; daftar dependensi.],
  [DOD-10], [Tidak ada kredensial pribadi dalam source/log dan tidak ada pengujian destruktif pada database aplikasi.], [Review keamanan dan pemisahan database test.],
  [DOD-11], [Cacat yang menghalangi alur Must ditutup; keterbatasan dan keputusan terbuka tercatat.], [Daftar cacat dan hasil regresi.],
  [DOD-12], [Dukungan OS/performa yang belum diuji tidak diklaim telah lulus.], [Matriks lingkungan dan hasil ukur jika target sudah disepakati.],
))

== Checklist persetujuan dokumen

#prd-table((39mm, 1fr, 29mm), ([Pihak], [Nama dan tanggal], [Status]), (
  [Pemilik produk], [Perlu Keputusan], [Belum disahkan],
  [Desainer/developer], [Perlu Keputusan], [Belum disahkan],
  [QA/perwakilan pengguna], [Perlu Keputusan], [Belum disahkan]
))

== Quality gate dokumen

Bagian ini menilai kualitas PRD, bukan kelulusan pengujian aplikasi. Review Anti Slop dilakukan setelah draf lengkap pada 20 Oktober 2026, dengan fokus pada kejujuran, keterlacakan, kelengkapan fungsi, aksesibilitas, dan bahasa yang dapat ditindaklanjuti. Dokumen dikompilasi dengan Typst 0.15.1 dan render seluruh halaman diperiksa.

#prd-table((34mm, 1fr, 18mm), ([Pemeriksaan], [Kriteria], [Hasil]), (
  [Batas sumber], [Identitas/fitur ETC ditelusuri ke OUTLINE, keputusan pengguna, DESIGN, dan source project. Domain dan data produk contoh tidak diadopsi.], [Lulus],
  [Kejelasan scope], [Bagian 6 memisahkan ruang lingkup; Bagian 18 memuat 10 keputusan terbuka. Usulan Should/Could diberi kedudukan jelas.], [Lulus],
  [Keterlacakan], [39 FR terpetakan ke 25 user story dengan AC dan 62 TC. FR-39 dan US-25 memuat notifikasi sukses beserta pengujian positif, negatif, serta penutupan pesan. Pemeriksaan ID tidak menemukan referensi TC yang hilang.], [Lulus],
  [Kejujuran angka], [Jumlah seed ditelusuri ke fixture source. Target performa/adopsi tidak dikarang; dukungan native memerlukan KD-02 dan bukti pengujian.], [Lulus],
  [Kelengkapan kualitas], [FR-05 dan FR-31 sampai FR-39, 20 NFR, serta TC terkait mencakup akses, validasi, state UI, aksesibilitas, dan integritas.], [Lulus],
  [Bahasa], [Istilah/status diperiksa pada FR, BR, US, dan TC. Tidak ada klaim promosi, testimoni, atau em dash; kebutuhan ditulis sebagai tindakan yang dapat diuji.], [Lulus],
  [Penyajian], [Render diperiksa: 19 bagian berurutan, tabel dengan header berulang, nomor halaman, dan tanpa pemotongan isi. Pemeriksaan kanal warna tidak menemukan piksel berwarna.], [Lulus],
  [Kompilasi], [Typst berhasil mengompilasi file mandiri tanpa package atau aset eksternal; hasil PDF dan ekstraksi teks diperiksa.], [Lulus],
))

Referensi substansi internal: OUTLINE.md; keputusan terbaru pengguna; DESIGN.md; README.md; schema pada `database`; model, service dan konfigurasi pada `src/Etc.Desktop`; serta skenario test project. Catatan audit UI terdahulu tidak diperlakukan sebagai bukti bahwa seluruh kebutuhan PRD ini otomatis lulus.
