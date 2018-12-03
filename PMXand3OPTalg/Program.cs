using System;
using System.Collections.Generic;
using Communications;
using MassTransit;
using PMXand3OPTalg.Queues;

namespace PMXand3OPTalg
{
    public class TspResult
    {
        public List<Location> BestTour { get; set; }
        public double Distance { get; set; }
        public double SolutionCount { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var bus = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                var host = sbc.Host(new Uri("rabbitmq://luozycyv:HMETMyUNp2qMHslxNUihuSMWLb6NElPy@hound.rmq.cloudamqp.com/luozycyv?temporary=true"),
                    h => { });
                sbc.ReceiveEndpoint(host, ep =>
                {
                    ep.Consumer<AlgorithmsConsumer>();
                });
            });
            bus.Start();
            Console.WriteLine("Abonent started");
            Console.ReadKey();
            bus.Stop();
        }
    }
}
