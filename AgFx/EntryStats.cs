using System.Collections.Generic;
using System.Linq;
using System;

namespace AgFx {

    /// <summary>
    /// Provides a container for the access stats for a CacheEntry
    /// </summary>
    internal class EntryStats {

        internal CacheEntry _cacheEntry;

        private List<double> _fetchTimes;
        private List<double> _deserializeTimes;
        private List<int> _deserializeSizes;
        private List<double> _updateTimes;

        public int RequestCount { get; private set; }
        public int FetchCount { get; private set; }
        public int FetchFailCount { get; set; }
        public int DeserializeFailCount { get; set; }

        public double CacheHitRatio {
            get {
                if (RequestCount == 0) return 0;
                return (RequestCount - FetchCount) / (double)RequestCount;
            }
        }

        public double MaxFetchTime {
            get {
                if (_fetchTimes == null) {
                    return 0;
                }
                return _fetchTimes.Max();
            }
        }

        public double MinFetchTime {
            get {
                if (_fetchTimes == null) return 0;
                return _fetchTimes.Min();
            }
        }

        public double AverageFetchTime {
            get {
                if (_fetchTimes == null) return 0;
                return _fetchTimes.Average();
            }
        }

        public double MaxDeserializeTime {
            get {
                if (_deserializeTimes == null) {
                    return 0;
                }
                return _deserializeTimes.Max();
            }
        }

        public double MinDeserializeTime {
            get {
                if (_deserializeTimes == null) return 0;
                return _deserializeTimes.Min();
            }
        }

        public double AverageDeserializeTime {
            get {
                if (_deserializeTimes== null) return 0;
                return  _deserializeTimes.Average();
            }
        }

        public int MinDataSize {
            get {
                if (_deserializeSizes == null) return 0;
                return _deserializeSizes.Min();
            }
        }

        public int MaxDataSize {
            get {
                if (_deserializeSizes == null) return 0;
                return _deserializeSizes.Max();
            }
        }

        public double AverageDataSize {
            get {
                if (_deserializeSizes == null) return 0;
                return _deserializeSizes.Average();
            }
        }

        public double UpdateCount {
            get {
                if (_updateTimes == null) return 0;
                return _updateTimes.Count();
            }
        }

        public double AverageUpdateTime {
            get {
                if (_updateTimes == null) return 0;
                return _updateTimes.Average();
            }
        }

        public double MaxUpdateTime {
            get {
                if (_updateTimes == null) return 0;
                return _updateTimes.Max();
            }
        }

        public EntryStats(CacheEntry entry) {
            _cacheEntry = entry;
        }

        DateTime? _deserializeStartTime;

        public void OnStartDeserialize() {

            if (!DataManager.ShouldCollectStatistics) return;
                        
            _deserializeStartTime = DateTime.Now;
        }

        public void OnCompleteDeserialize(int dataSize) {

            if (!DataManager.ShouldCollectStatistics) return;

            if (_deserializeStartTime == null) {                
                return;
            }
            var time = DateTime.Now.Subtract(_deserializeStartTime.Value).TotalMilliseconds;
            _deserializeStartTime = null;
            if (_deserializeTimes == null) {
                _deserializeTimes = new List<double>();
            }

            _deserializeTimes.Add(time);

            if (_deserializeSizes == null) {
                _deserializeSizes = new List<int>();
            }
            _deserializeSizes.Add(dataSize);
        }

        public void Reset() {
            RequestCount = 0;
            FetchCount = 0;
            DeserializeFailCount = 0;
            _fetchTimes = null;
            _deserializeTimes = null;
            _deserializeSizes = null;
            _updateTimes = null;
        }

        public void OnRequest() {

            if (!DataManager.ShouldCollectStatistics) return;

            RequestCount++;
        }

        DateTime? _fetchStartTime;

        public void OnStartFetch() {

            if (!DataManager.ShouldCollectStatistics) return;
            _fetchStartTime = DateTime.Now;
        }

        public void OnCompleteFetch(bool success) {

            if (!DataManager.ShouldCollectStatistics) return;

            if (!_fetchStartTime.HasValue) {            
                return;
            }

            var time = DateTime.Now.Subtract(_fetchStartTime.Value).TotalMilliseconds;
            _fetchStartTime = null;
            FetchCount++;
            if (!success) {
                FetchFailCount++;
            }
            if (_fetchTimes == null) {
                _fetchTimes = new List<double>();
            }
            _fetchTimes.Add(time);
        }


        internal void OnDeserializeFail() {
            DeserializeFailCount++;
        }

        DateTime? _updateStart;

        internal void OnStartUpdate() {
            _updateStart = DateTime.Now;
        }

        internal void OnCompleteUpdate() {
            if (_updateStart == null) {
                return;
            }

            var time = DateTime.Now.Subtract(_updateStart.Value).TotalMilliseconds;
            _updateStart = null;
            if (_updateTimes == null) {
                _updateTimes = new List<double>();
            }
            _updateTimes.Add(time);
        }
    }
}
