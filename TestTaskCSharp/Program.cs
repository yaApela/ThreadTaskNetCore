using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace TestTaskCSharp
{
    class Program
    {
        static Mutex mutex = new Mutex(); // Создание мьютекса
        static void getIdThread()
        {
            Console.WriteLine($"ID текущего потока: {Thread.CurrentThread.ManagedThreadId}");
        }
        static void startProccess(string proccessName, ref List<Service> processes) 
        {
            mutex.WaitOne();
            getIdThread();
            Service service = new Service(proccessName);
            service.Start();
            processes.Add(service);
            mutex.ReleaseMutex();
        }
        static void viewProcessInformation(List<Service> services)
        {
            Console.Clear();
            foreach (var service in services)
            {
                if (service != null)
                {
                    if (service is Service backgroundService)
                    {
                        backgroundService.IsServiceRunning();
                        Console.WriteLine($"Название сервиса: {service.servesName}");
                        // Проверьте, работает ли сервис
                        if (backgroundService.IsRunning)
                        {
                            Console.WriteLine("Статус сервиса: RUNNING");
                        }
                        else
                        {
                            Console.WriteLine("Статус сервиса: STOPPED");
                        }
                        Console.WriteLine($"Уведомлять об остановке сервиса: {service.process.StartInfo.UseShellExecute.ToString()}");
                        Console.WriteLine($"Перенаправлять стандартный вывод сервиса: {service.process.StartInfo.RedirectStandardOutput.ToString()}");
                        Console.WriteLine($"\n{new string('-',50)}\n");
                    }
                    else
                    {
                        Console.WriteLine("Сервис не является MySevice");
                    }
                }
                else
                {
                    Console.WriteLine("Сервис не найден");
                }
            }
            Thread.Sleep(2000);
            viewProcessInformation(services);
        }
        static void Main(string[] args)
        {
            List<Service> processes = new List<Service>();
            string[] proccessNames = new string[3] { "com.docker.service", "winlogon", "aspnet_state" };
            Parallel.Invoke(
                () => startProccess(proccessNames[0], ref processes),
                () => startProccess(proccessNames[1], ref processes),
                () => startProccess(proccessNames[2], ref processes)
                );

            Thread.Sleep(3000); // 3-х секндная задержка чтобы был виден вывод того что все запущено в разных потоках(сделал ток для того чтобы можно было это увидеть)

            viewProcessInformation(processes); // возлагаю вывод данных о сервисах на основной поток
        }
    }
}
class Service
{
    private bool isRunning = false;
    public bool IsRunning => isRunning;
    public string servesName;
    public Process[] processes;
    public Process process;
    public Service(string ServesName)
    {
        servesName = ServesName;
    }
    public Task Start()
    {
        process = new Process();
        process.StartInfo.FileName = "net";
        process.StartInfo.Arguments = $"start {servesName}";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        process.WaitForExit();
        isRunning = true;
        return Task.CompletedTask;
    }
    public Task Stop()
    {
        process.Kill();
        isRunning = false;
        return Task.CompletedTask;
    }
    public bool IsServiceRunning()
    {
        processes = Process.GetProcessesByName(servesName);
        if (processes.Length > 0)
            isRunning = true;
        else
            isRunning = false;

        return IsRunning;
    }
}

