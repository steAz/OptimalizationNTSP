using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Communications;
using MassTransit;
using TSP;

namespace PMXand3OPTalg.Queues
{
    class AlgorithmsConsumer : IConsumer<IAlgortihmsInfo>
    {
        private static int _numberOfTasks = 0;

        public Task Consume(ConsumeContext<IAlgortihmsInfo> ctx)
        {
            _numberOfTasks++;
            Console.WriteLine("Number of task = " + _numberOfTasks);
            var bestTspRes = new TspResult();
            var tspDataFilePath = ctx.Message.TspDataFilePath;
            var dataModel = new DataModel(tspDataFilePath);
            double bestDistance = Double.MaxValue;
            List<Location> tempTour = new List<Location>(dataModel.Data);
            List<Location> tempTourSec = new List<Location>(dataModel.Data);
            double tempDistance = 0;
            int counter = 0;
            var phaseFirstTimeInSeconds = ctx.Message.PhaseFirstTimeInSeconds;
            var phaseSecTimeInSeconds = ctx.Message.PhaseSecTimeInSeconds;

            tempTour.Shuffle(); // first parent
            tempTourSec.Shuffle(); // sec parent

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed.TotalSeconds < phaseFirstTimeInSeconds)
            {
                counter++;
                if (counter % 2 == 0)
                {
                    tempTour.Shuffle();
                    if ((tempDistance = Utils.DistanceSum(tempTour)) < bestDistance)
                    {
                        bestTspRes.BestTour = new List<Location>(tempTour);
                        bestDistance = tempDistance;
                        RespondResults(ctx, bestTspRes.BestTour, bestDistance, counter);
                    }
                }
                else
                {
                    tempTourSec.Shuffle();
                    if ((tempDistance = Utils.DistanceSum(tempTourSec)) < bestDistance)
                    {
                        bestTspRes.BestTour = new List<Location>(tempTourSec);
                        bestDistance = tempDistance;
                        RespondResults(ctx, bestTspRes.BestTour, bestDistance, counter);
                    }
                }
            }

            Console.WriteLine("Second stage started ...");
            sw.Restart();
            while (sw.Elapsed.TotalSeconds < phaseSecTimeInSeconds)
            {
                counter++;

                tempTour.SwapEdges();
                if ((tempDistance = Utils.DistanceSum(tempTour)) < bestDistance)
                {
                    bestTspRes.BestTour = new List<Location>(tempTour);
                    bestDistance = tempDistance;
                    RespondResults(ctx, bestTspRes.BestTour, bestDistance, counter);
                }
            }
            
            Console.WriteLine("the shortest found traveling salesman route:");
            Console.WriteLine("Distance = " + bestDistance);
            Console.WriteLine("Solution Count = " + counter);
            Console.WriteLine("Task ID = " + ctx.Message.TaskId);

            RespondResults(ctx, bestTspRes.BestTour, bestDistance, counter);

            return Task.FromResult(0);
        }

        public void RespondResults(ConsumeContext<IAlgortihmsInfo> ctx, 
                                    List<Location> bestTour,
                                    double bestDistance, int counter)
        {
            ctx.RespondAsync(new ResultsInfo()
            {
                BestTour = bestTour,
                BestDistance = bestDistance,
                TaskId = ctx.Message.TaskId,
                SolutionCount = counter,
            });
        }
    }
}
