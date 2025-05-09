﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Management;

namespace ConsoleApp1
{
	class Program
	{
		static int timeOutDakika = 10;

		static void Main(/*string[] args*/)
		{
			Task.Factory.StartNew(() =>
			{
				Job1();
			});

            Console.WriteLine($"{timeOutDakika} dakika internet kesilir ise sunucu yeniden başlatılır.");
			Console.ReadLine();

			


        }

		static void LogSave(string text)
		{
			try
			{
				string logDosyaAdi = "c:\\aktif_logs" + "_" + DateTime.Now.ToString("yyyy.MM.dd") + ".txt";
				StreamWriter dosya = File.AppendText(logDosyaAdi);
				dosya.WriteLine(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff") + " " + text);
				dosya.Close();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Log kayıt edilemedi: {ex.Message}");
			}
		}

		// İnternet bağlantısını kontrol et
		static bool IsInternetAvailable()
		{
			try
			{
				string pingTarget = "8.8.8.8"; // Google DNS, bağlantıyı kontrol etmek için
				using (Ping ping = new Ping())
				{
					var reply = ping.Send(pingTarget, 2000); // 2 saniye timeout
					return reply.Status == IPStatus.Success;
				}
			}
			catch
			{
				// Ping hata verirse, bağlantı yok demektir
				return false;
			}
		}

		// Sunucuyu yeniden başlat
		static void RestartServer()
		{
			try
			{
				Process.Start("shutdown", "/r /f /t 0");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Sunucu yeniden başlatılamadı: {ex.Message}");
			}
		}

        static void RestartNetworkAdapter()
        {
            try
            {
                string adapterName = "Ethernet"; // Ağ adaptörünün adı
                string query = $"SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionID = '{adapterName}'";

                string message0 = $"Restart Network Adapter ({adapterName}) Başlatıldı: {DateTime.Now}";
                LogSave(message0);
                Console.WriteLine(message0);

                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                foreach (ManagementObject adapter in searcher.Get())
                {
                    adapter.InvokeMethod("Disable", null);
                    string message1 = $"Network Adapter ({adapterName}) Devre dışı bırakıldı...: {DateTime.Now}";
                    LogSave(message1);
                    Console.WriteLine(message1);

                    Thread.Sleep(3000);

                    adapter.InvokeMethod("Enable", null);
                    string message2 = $"Network Adapter ({adapterName}) Etkinleştirildi. {DateTime.Now}";
                    LogSave(message2);
                    Console.WriteLine(message2);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
            }
        }

        static void Job1()
		{
			int timeout = 1000 * 60 * timeOutDakika; // timeOutDakika:10 dakika (10 dk = 600000 milisaniye)
			DateTime connectionLostTime = DateTime.Now;

			LogSave("Internet bağlantısı kontrolü görevi başladı.");

			while (true)
			{
				// Bağlantıyı kontrol et (Ping at)
				if (IsInternetAvailable())
				{
					connectionLostTime = DateTime.Now;
					string message = $"Internet bağlantısı var: {DateTime.Now}";
					LogSave(message);
					//Console.WriteLine(message);
				}
				else
				{
					var gecenSure = (DateTime.Now - connectionLostTime);
					// Eğer timeout dakika geçtiyse, sunucuyu yeniden başlat
					if (gecenSure.TotalMilliseconds >= timeout)
					{
						string message = $"{gecenSure.TotalMinutes} dk geçti, sunucu yeniden başlatılıyor...";
						LogSave(message);
						Console.WriteLine(message);
						RestartServer();
						break; // Programı sonlandır
					}
					else
					{
						string message = $"Internet bağlantısı kayboldu: {DateTime.Now}";
						LogSave(message);
						Console.WriteLine(message);

						// network ü yeniden başlat
						RestartNetworkAdapter();
                        Thread.Sleep(1000 * 60 * 1);
                    }
                }


				// 1 dk bekle ve tekrar kontrol et
				Thread.Sleep(1000 * 60 * 1);
			}
		}

	}
}
