namespace StudentGearHub.API.IRepository
{
    public class ServiceResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    public class ServiceResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }
}