namespace FitWifFrens.Api.Dtos
{
    public class CommitmentDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public int Days { get; set; }
        public string ContractAddress { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }
}
