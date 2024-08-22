using FitWifFrens.Api.Dtos;
using FitWifFrens.Data;

namespace FitWifFrens.Api.Mappers
{
    public static class CommitmentMappers
    {
        public static CommitmentDto ToCommitmentDto(this Commitment commitment)
        {
            return new CommitmentDto()
            {
                Id = commitment.Id,
                Title = commitment.Title,
                Description = commitment.Description,
                Image = commitment.Image,
                StartDate = commitment.StartDate,
                Days = commitment.Days,
                ContractAddress = commitment.ContractAddress,
                Balance = commitment.Balance
            };
        }
    }
}
