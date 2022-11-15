using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net.Http;
using HtmlAgilityPack.CssSelectors.NetCore;
using System.Net;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Text;

namespace SanctionScanner
{
  class Program
  {

    static Dictionary<string, string> titlePriceListe = new Dictionary<string, string>(); // Başlıkları ve Ücretleri aynı anda göstermek için dictionary tanımladım(Key,Value).
    static HtmlDocument html;                                                             // Bir HTML' e erişim sağlar.
    static List<string> postDetail = new List<string>();                                  // Vitrindeki ilanların detayına gitmek için, string tipinde oluşturulmuş liste.
    static List<string> postTitle = new List<string>();                                   // Vitrindeki detaylara gidilen ilanların başlıklarını tutmak için string tipinde oluşturulmuş liste.
    static List<string> postPrice = new List<string>();                                   // Vitirindeki detaylara gidilen ilanların ücretlerini tutmak için string tipinde oluşturulmuş liste.
    static decimal totalPriceAverage = 0;                                                 // İlan fiyatlarnın ortalamasını tutacak decimal tipinde bir değer. Decimal kullandım çünkü bölme işlemi yapacağımız için ondalıklı gelebilir.
    static  void Main(string[] args)
    {
      TitleHtml("https://www.sahibinden.com");
      PriceHtml("https://www.sahibinden.com");
      ShowInConsole();
      WriteToDisk(@"C:");

      // Oluşturulan tüm fonksiyonlar Main Metodu içinde başalatılır.
    }


    public static void TitleHtml(string link)
    {
      string htmlPage;

      HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);                   // HTTP isteklerini kullanmak için kullanmak için bu sınıftan bir ınstance oluşturduk.
      request.Timeout = 7000;                                                             // 7 saniye de bir istek yolluyoruz çünkü zaman aşımına uğramayalım.
      WebResponse response = request.GetResponse();                                       // Web browserdan dönen yanıtı sağlar.
      using (Stream responseStream = response.GetResponseStream())                        // Stream verinin bir bütün değil de parça parça alınması,işlenmesi olarak düşünülebilir.
      {
        StreamReader reader = new StreamReader(responseStream, encoding: Encoding.UTF8);  // Parça parça gelen veri reader tarafından okumaya hazırlandı, encoding yapılarak.
        htmlPage = reader.ReadToEnd();                                                    // Oluşturduğumuz değişkene okunan data aktarılıdı.
      }
      using (WebClient client = new WebClient())                                          // URI ile tanımlanan bir kaynağa veri göndermek ve kaynaktan veri almak kullanılan bir sınıftır.
      {
        client.Proxy = new WebProxy("https://www.sahibinden.com/:8080",true);             // Http atmak için proxy ayarları yapıldı.
        html = new HtmlAgilityPack.HtmlDocument();                                        // HTML, PHP, ASPX gibi içerikleri ayrıştırmanızı düzenleminizi kullanmanızı sağlayan bir kütüphanedir
        html.LoadHtml(htmlPage);                                                          // Yukarıda oluşturduğumuz değişkene sayfa kaynağını atadığımız değişkene yüklüyoruz.
        HtmlNode[] nodes = html.DocumentNode.SelectNodes(@"//*[@id=""container""]/ div[3]/div/div[3]/div[3]/ul/li/a").ToArray(); // Yüklenen html içerisinden ilgili path'e gidiliyor(Detayına)
        foreach (var item in nodes)                                                       // Gidilen herbir detaydaki item için  başlıklar alınıp ilgili listeye ekleniyor.
        {
          if (item.Attributes["href"].Value.Contains("/ilan"))
          {
            postTitle.Add(item.InnerText);
          }
          else                                                                            // Reklam kontrolu yapılıyor.
          {
            Console.WriteLine("burası reklam olan satır");
          }
        }
      }
    }
    public static void PriceHtml(string link)
    {
      string htmlDetailPage;
      for (int i = 0; i < postDetail.Count; i++)
      {
        HttpWebRequest requestForDetail = (HttpWebRequest)WebRequest.Create(link + postDetail[i].ToString()); // linki detay linki ile birleştirdim.
        requestForDetail.Timeout = 7000;                                                                      // 7 saniye de bir istek yolluyoruz çünkü zaman aşımına uğramayalım.
        WebResponse responseForDetail = requestForDetail.GetResponse();                                       // Web browserdan dönen yanıtı sağlar.
        using (Stream responseForDetailStream = responseForDetail.GetResponseStream())                        
        {
          StreamReader reader = new StreamReader(responseForDetailStream, encoding: Encoding.UTF8);           // Parça parça gelen veri reader tarafından okumaya hazırlandı, encoding yapılarak.
          htmlDetailPage = reader.ReadToEnd();
        }
        using (WebClient clientForDetail = new WebClient())
        {
          clientForDetail.Proxy = new WebProxy("https://www.sahibinden.com/:8080", true);                     // Http atmak için proxy ayarları yapıldı.
          html = new HtmlDocument();                                                                          // HTML, PHP, ASPX gibi içerikleri ayrıştırmanızı düzenleminizi kullanmanızı sağlayan bir kütüphanedir
          html.LoadHtml(htmlDetailPage);                                                                      // Yukarıda oluşturduğumuz değişkene sayfa kaynağını atadığımız değişkene yüklüyoruz.
          var nodesForDetail = html.DocumentNode.SelectSingleNode(@"//*[@id=""favoriteClassifiedPrice""]");   // Yüklenen html içerisinden ilgili path'e gidiliyor(Detayına)
          if (nodesForDetail == null)
          {
            Console.WriteLine("Reklam İçeriği");
          }
          else
          {
            postPrice.Add(nodesForDetail.Attributes["value"].Value);
            postPrice[i] = postPrice[i].Replace("TL", "");                                                    // TL yazan "" ile yer değiştiriyoruz.
            postPrice[i] = postPrice[i].Replace(".", "");                                                     // . yazan "" ile yer değiştiriyoruz.
          }
        }
      }
      
    }
    public static void ShowInConsole()
    {
      for (int i = 0; i < postPrice.Count; i++)                           // Yukarıda listeye atılan ücret ve başlık bilgi ekrana yazdırılıyor
      {
        Console.WriteLine(postTitle[i] + ":" + postPrice[i]);
        titlePriceListe.Add(postTitle[i], postPrice[i]);                  // Ücret ve başlık bilgisi dictionary bir liste ekleniyor.
      }
      if (postPrice.Count > 0)
      {
        foreach (var item in postPrice)
        {
          totalPriceAverage = totalPriceAverage + Convert.ToDecimal(item); // Toplam ücret convert edilip yukarıda atadığımız değişkene verip hazır bekletiyoruz.
        }
      }
      Console.WriteLine("Average:" + (totalPriceAverage/Convert.ToDecimal(postPrice.Count)).ToString() + "TL"); // Toplam ücreti priceın Count'na bölerek ortalama ücreti buluyoruz.
    }
    public static void WriteToDisk(string path)
    {
      FileStream fileStream = File.Create(path + "\\" + "sahibindenInfo.txt");                                          // Dosya yolu oluşturmak için kullanılmaktadır.
      StreamWriter streamWriter = new StreamWriter(fileStream);                                                         // Oluşturulan dosya yoluna yazmak için kullanılır.
      foreach (var item in titlePriceListe)
      {
        streamWriter.WriteLine(item.Key.Trim() + ":" + item.Value.Trim());                                              // foreach döngüsü ile başılk ve fiyat bilgisi dosya yolunda bulunan pathe yazılıyor.
      }

      streamWriter.WriteLine("Average:" + (totalPriceAverage / Convert.ToDecimal(postPrice.Count)).ToString() + "TL"); // Extra olarak Ücret ortalamsaı tekrardan yazılıyor.
      streamWriter.Close();                                                                                            // streamwriter kapatılıyor.
      fileStream.Close();                                                                                              // filestream kapatılıyor.
    }
  }
}





//private static async void GetHtmlAsync()
//{

//  string url = "https://www.ebay.com/sch/i.html?_from=R40&_trksid=p2380057.m570.l1313&_nkw=pc&_sacat=0";
//  TextWriter txtPrice = new StreamWriter("C:\\Users\\oguzh\\Desktop\\txtPrice.txt");
//  decimal toplam = 0;

//  var httpClient = new HttpClient();
//  var html = await httpClient.GetStringAsync(url);

//  var htmlDocument = new HtmlDocument();
//  htmlDocument.LoadHtml(html);

//  var productPrice = htmlDocument.DocumentNode.Descendants("span")
//    .Where(node => node.GetAttributeValue("class", "").Equals("s-item__price")).ToList();

//  var productName = htmlDocument.DocumentNode.Descendants("div")
//     .Where(node => node.GetAttributeValue("class", "").Equals("s-item__title")).ToList();


//  foreach (var item in productPrice)
//  {
//    Console.WriteLine(item.InnerText);
//    txtPrice.Write(item.InnerText);
//  }

//  txtPrice.Close();
//  foreach (var item in productName)
//  {
//    Console.WriteLine(item.InnerText);
//  }

//}