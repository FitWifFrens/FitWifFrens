using FitWifFrens.Api.Dtos;
using FitWifFrens.Api.Mappers;
using FitWifFrens.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitWifFrens.Api.Controllers
{
    [ApiController]
    [Route("commitments")]
    public class CommitmentControllor : Controller
    {
        private readonly DataContext _dataContext;
        public CommitmentControllor(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<CommitmentDto>))]
        public async Task<IActionResult> GetAllCommitments()
        {
            var commitments = await _dataContext.Commitments.ToListAsync();

            return Ok(commitments.Select(c => c.ToCommitmentDto()));
        }
    }
}
