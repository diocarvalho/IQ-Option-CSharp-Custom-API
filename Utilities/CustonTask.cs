using System.Threading.Tasks;

namespace IqApiNetCore.Utilities
{
    public class CustomTasker<T>
    {
        private T request;
        private T result;
        public enum Status
        {
            Created,
            WaitingResult,
            Completed
        }
        public Status status { get; private set; }

        public CustomTasker()
        {
            status = Status.Created;
        }
        public bool SetRequest(T request)
        {
            if (status == Status.Created)
            {
                this.request = request;
                status = Status.WaitingResult;
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool SetResult(T result)
        {
            if (status == Status.WaitingResult)
            {
                this.result = result;
                status = Status.Completed;
                return true;
            }
            else
            {
                return false;
            }
        }
        public T Request
        {
            get
            {
                return request;
            }
        }
        public T Result
        {
            get
            {
                task.Wait();
                return result;
            }
        }
        public Task<T> task
        {
            get
            {
                return GetTask();
            }
        }
        private async Task<T> GetTask()
        {
            while (status != Status.Completed) { await Task.Delay(50); }
            return result;
        }
    }
}
