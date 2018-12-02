using System;
using System.Collections.Generic;

namespace Communications
{
    public class Location : IEquatable<Location>
    {
        public int Id { get; set; }
        public Double X { get; set; }
        public Double Y { get; set; }// override object.Equals
        public Double ViewX { get; set; }
        public Double ViewY { get; set; }


        public bool Equals(Location other)
        {
            return (this.Id == other.Id);
        }
    }

    public interface IAlgortihmsInfo
    {
        int PhaseFirstTimeInSeconds { get; }
        int PhaseSecTimeInSeconds { get; }
        int? TaskId { get; }
        int NumberOfTasks { get; }
        string TspDataFilePath { get; }
    }

    public interface IResultsInfo
    {
        double BestDistance { get; }
        int SolutionCount { get; }
        int? TaskId { get; }
        List<Location> BestTour { get; }
    }

    public class AlgorithmsInfo : IAlgortihmsInfo
    {
        public int PhaseFirstTimeInSeconds { get; set; }
        public int PhaseSecTimeInSeconds { get; set; }
        public int? TaskId { get; set; }
        public string TspDataFilePath { get; set; }
        public int NumberOfTasks { get; set; }
    }

    public class ResultsInfo : IResultsInfo
    {
        public double BestDistance { get; set; }
        public int SolutionCount { get; set; }
        public int? TaskId { get; set; }
        public List<Location> BestTour { get; set; }
    }
}
