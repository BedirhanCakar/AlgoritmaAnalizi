# AWS (Amazon Web Services) Üzerinde Docker ile Canlıya Alma (Deployment) Kılavuzu

Bu kılavuz, projenin **AWS Frankfurt (eu-central-1)** bölgesinde **Docker** ve **AWS Elastic Beanstalk (ya da ECS)** kullanılarak profesyonel standartlarda nasıl yayına alınacağını adım adım anlatmaktadır.

---

## 1. Mimari Tasarım

AWS üzerinde ücretsiz/düşük bütçeli ve verimli bir altyapı kurmak için aşağıdaki bileşenler kullanılacaktır:

*   **Uygulama Sunucusu:** **AWS Elastic Beanstalk (Docker Platformu)** veya **AWS App Runner** (t3.micro/t3.small EC2 sunucusu ile ücretsiz paket kapsamında).
*   **Veritabanı:** 
    *   *Seçenek A (Düşük Maliyet/Test):* `docker-compose.yml` ile aynı EC2 üzerinde çalışan SQL Server Konteyneri (önerilen).
    *   *Seçenek B (Prodüksiyon/Yüksek Güvenlik):* **AWS RDS (Relational Database Service) SQL Server Express Edition** (ücretsiz deneme kapsamındadır, ancak saatlik limitleri kontrol edilmelidir).
*   **Yetkilendirme ve E-posta API:** Ortam Değişkenleri (Environment Variables) aracılığıyla güvenli şekilde enjekte edilen SMTP E-posta kimlik bilgileri.

---

## 2. Docker Hazırlıkları

Projenin kök dizininde yer alan `docker-compose.yml` dosyası canlı ortam (Production) için optimize edilmiştir:

```yaml
version: '3.8'

services:
  web:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "80:8080" # Canlı ortam için HTTP (Port 80) üzerinden yönlendirme yapıldı
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Server=db;Database=ExamSchedulerDb;User Id=sa;Password=Your_Strong_Password_123!;TrustServerCertificate=True;
      # E-posta SMTP Ayarları (Ortam Değişkeni olarak geçilir)
      - Email__SmtpServer=${EMAIL_SMTP_SERVER}
      - Email__Port=${EMAIL_PORT}
      - Email__SenderEmail=${EMAIL_SENDER_EMAIL}
      - Email__SenderPassword=${EMAIL_SENDER_PASSWORD}
      - Email__EnableSsl=${EMAIL_ENABLE_SSL}
    depends_on:
      db:
        condition: service_healthy

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=Your_Strong_Password_123!
    volumes:
      - mssql_data:/var/opt/mssql/data
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Your_Strong_Password_123!' -C -Q 'SELECT 1' || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  mssql_data:
```

---

## 3. AWS Elastic Beanstalk ile Yayına Alma Adımları

AWS Elastic Beanstalk, Docker uygulamalarını otomatik olarak kuran, ölçekleyen ve yük dengelemesini yöneten en pratik AWS servisidir.

### Adım 1: AWS Hesabı ve IAM Rolleri
1.  [AWS Console](https://aws.amazon.com) adresine giriş yapın.
2.  Frankfurt (`eu-central-1`) bölgesini seçin.
3.  IAM servisine giderek `aws-elasticbeanstalk-ec2-role` rolünün oluşturulduğundan ve `AWSElasticBeanstalkWebTier` politikasının tanımlı olduğundan emin olun.

### Adım 2: Elastic Beanstalk Uygulaması Oluşturma
1.  Arama çubuğuna **Elastic Beanstalk** yazın ve servise girin.
2.  **Create Application** butonuna tıklayın.
3.  **Application Name:** `ExamScheduler` yazın.
4.  **Platform:** **Docker** seçin.
5.  **Platform Branch:** **Docker running on 64bit Amazon Linux 2023** seçin.
6.  **Application Code:** **Sample Application** seçerek devam edin (Kendi kodumuzu konfigürasyondan sonra yükleyeceğiz).
7.  **Presets:** **Single Instance (Free Tier Eligible)** seçin. Bu seçenek t2.micro/t3.micro ücretsiz EC2 sunucusunu kullanır.

### Adım 3: Sunucu (EC2) ve Veritabanı Güvenlik Ayarları
1.  Konfigürasyon ekranında **Configure updates, monitoring, and logging** bölümüne gidin.
2.  **Environment properties** (Ortam değişkenleri) alanına gelerek SMTP E-posta bilgilerinizi girin:
    *   `EMAIL_SMTP_SERVER` = *(SMTP Sunucu Adresi, örn: smtp.gmail.com)*
    *   `EMAIL_PORT` = *(SMTP Portu, örn: 587)*
    *   `EMAIL_SENDER_EMAIL` = *(Gönderici E-posta Adresi)*
    *   `EMAIL_SENDER_PASSWORD` = *(Gönderici E-posta Şifresi veya Uygulama Şifresi)*
    *   `EMAIL_ENABLE_SSL` = *(SSL aktif mi? true/false)*
3.  Değişiklikleri kaydedip ortamın (environment) ayağa kalkmasını bekleyin.

### Adım 4: Kodu Paketleme ve Yükleme
Uygulamayı Beanstalk'a yüklemek için yerel dizininizdeki kodları ziplemeniz gerekir:
1.  Proje kök dizinindeki dosyaları seçin (Özellikle `Dockerfile`, `docker-compose.yml`, `Controllers`, `Models`, `Views`, `Program.cs` ve `.csproj` dosyaları dahil edilmelidir. `bin`, `obj` ve `.git` klasörleri hariç tutulabilir).
2.  Seçilen dosyaları sağ tıklayıp `ExamScheduler.zip` olarak sıkıştırın.
3.  AWS EB Arayüzünden **Upload and Deploy** butonuna basın.
4.  `ExamScheduler.zip` dosyasını seçip **Deploy** deyin.

AWS, Dockerfile'ı kullanarak imajı sunucu üzerinde derleyecek, `db` ve `web` servislerini docker-compose ile otomatik olarak ayağa kaldıracaktır.

---

## 4. AWS Üzerinde SSL/HTTPS ve Domain Ekleme

Profesyonel bir görünüm ve güvenlik için HTTPS kullanımı zorunludur:

1.  **Domain Yönlendirme:** Satın aldığınız alan adını (Domain) AWS **Route 53** servisine kaydederek Beanstalk URL'sine (`xxxxx.elasticbeanstalk.com`) CNAME olarak yönlendirin.
2.  **SSL Sertifikası (Ücretsiz):** **AWS Certificate Manager (ACM)** servisine giderek domain adresiniz için ücretsiz bir SSL sertifikası talep edin.
3.  **Yük Dengeleyici (Load Balancer):** Beanstalk ortam ayarlarından Load Balancer ekleyin. Port 443 (HTTPS) dinleyicisini aktif edip ACM'den aldığınız SSL sertifikasını tanımlayın.

---

## 5. Sıfır Kesinti (Zero-Downtime) Dağıtımı

Canlı ortama yeni güncellemeler gönderirken sistemin kesintiye uğramaması için:
*   Beanstalk **Deployment Policy** ayarlarından **Rolling with additional batch** veya **Immutable** seçeneğini aktif edin.
*   Bu sayede yeni sürüm yüklenirken önce yeni bir geçici sunucu oluşturulur, kod orada test edilir ve başarılı olursa canlı trafik oraya aktarılır. Kullanıcılar hiçbir kesinti hissetmez.
