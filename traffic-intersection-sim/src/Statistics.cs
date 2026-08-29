using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Добавьте эту директиву
namespace TrafficSimulation
{
    public class Statistics
    {
        private readonly List<double> _waits = new List<double>();
        private int _total;
        public void RecordCarPassed(double w) { _waits.Add(w); _total++; }
        public void SaveToFile(string path)
        {
            try
            {
                using var w = new StreamWriter(path);
                w.WriteLine($"=== Отчёт от {DateTime.Now:dd.MM.yyyy HH:mm} ===");
                w.WriteLine($"Проехало: {_total} авто");
                double avg = _waits.Count > 0 ? _waits.Average() : 0;
                w.WriteLine($"Среднее ожидание: {avg:F2} сек");
                if (_waits.Count > 0)
                {
                    w.WriteLine("\nГистограмма:");
                    foreach (var g in _waits.GroupBy(x => (int)x).OrderBy(x => x.Key).Take(10))
                        w.WriteLine($"{g.Key:00}-{(g.Key + 1):00}: {g.Count()}");
                }
            }
            catch { }
        }
    }
}
