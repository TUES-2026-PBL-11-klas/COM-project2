using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PM.Data.Context;
using PM.Core.DTOs;
using PM.Data.Entities;

namespace PM.API.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReviewsController(AppDbContext db)
        {
            _db = db;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
        {
            Guid reviewerId = Guid.Empty;
            if (dto.ReviewerId.HasValue)
                reviewerId = dto.ReviewerId.Value;

            if (reviewerId == Guid.Empty && User?.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name;
                var u = _db.Users.FirstOrDefault(x => x.Username == username);
                if (u != null) reviewerId = u.Id;
            }

            if (reviewerId == Guid.Empty) return BadRequest("reviewerId or auth required");
            if (string.IsNullOrWhiteSpace(dto.ReviewedId)) return BadRequest("reviewedId required");

            // parse reviewed id as guid or treat as external id
            Guid reviewedGuid = Guid.Empty;
            var isGuid = Guid.TryParse(dto.ReviewedId, out reviewedGuid);

            // prevent duplicate: same reviewer -> same reviewed
            var exists = _db.Reviews.Any(r => r.ReviewerId == reviewerId &&
                ((isGuid && r.ReviewedUserId.HasValue && r.ReviewedUserId.Value == reviewedGuid) ||
                 (!isGuid && r.ReviewedExternalId == dto.ReviewedId)));

            if (exists) return Conflict("Review already exists");

            var review = new Review
            {
                ReviewerId = reviewerId,
                ReviewerName = string.IsNullOrWhiteSpace(dto.ReviewerName) ? "Anonymous" : dto.ReviewerName,
                ReviewedUserId = isGuid ? reviewedGuid : (Guid?)null,
                ReviewedExternalId = isGuid ? null : dto.ReviewedId,
                Rating = dto.Rating,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReviewsFor), new { reviewedId = dto.ReviewedId }, new OutReviewDto
            {
                Id = review.Id,
                ReviewerId = review.ReviewerId,
                ReviewerName = review.ReviewerName,
                ReviewedUserId = review.ReviewedUserId,
                ReviewedExternalId = review.ReviewedExternalId,
                ReviewedUserName = review.ReviewedUserId.HasValue ? _db.Users.FirstOrDefault(u => u.Id == review.ReviewedUserId.Value)?.Username : null,
                Rating = review.Rating,
                Content = review.Content,
                CreatedAt = review.CreatedAt
            });
        }

        [AllowAnonymous]
        [HttpGet("{reviewedId}")]
        public IActionResult GetReviewsFor(string reviewedId)
        {
            if (string.IsNullOrWhiteSpace(reviewedId)) return BadRequest();

            Guid reviewedGuid = Guid.Empty;
            var isGuid = Guid.TryParse(reviewedId, out reviewedGuid);

            var q = _db.Reviews.AsQueryable();
            if (isGuid)
            {
                q = q.Where(r => r.ReviewedUserId.HasValue && r.ReviewedUserId.Value == reviewedGuid);
            }
            else
            {
                q = q.Where(r => r.ReviewedExternalId == reviewedId);
            }

            var outList = q.OrderByDescending(r => r.CreatedAt).Select(r => new OutReviewDto
            {
                Id = r.Id,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.ReviewerName,
                ReviewedUserId = r.ReviewedUserId,
                ReviewedExternalId = r.ReviewedExternalId,
                ReviewedUserName = r.ReviewedUserId.HasValue ? _db.Users.Where(u => u.Id == r.ReviewedUserId.Value).Select(u => u.Username).FirstOrDefault() : null,
                Rating = r.Rating,
                Content = r.Content,
                CreatedAt = r.CreatedAt
            }).ToList();

            return Ok(outList);
        }

        [Authorize]
        [HttpGet("authored")]
        public IActionResult GetAuthored()
        {
            var username = User?.Identity?.Name;
            if (username == null) return Unauthorized();
            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return Unauthorized();

            var q = _db.Reviews.Where(r => r.ReviewerId == user.Id || r.ReviewerName == username)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new OutReviewDto
                {
                    Id = r.Id,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.ReviewerName,
                    ReviewedUserId = r.ReviewedUserId,
                    ReviewedExternalId = r.ReviewedExternalId,
                    ReviewedUserName = r.ReviewedUserId.HasValue ? _db.Users.Where(u => u.Id == r.ReviewedUserId.Value).Select(u => u.Username).FirstOrDefault() : null,
                    Rating = r.Rating,
                    Content = r.Content,
                    CreatedAt = r.CreatedAt
                }).ToList();

            return Ok(q);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(Guid id, [FromBody] CreateReviewDto dto)
        {
            var username = User?.Identity?.Name;
            if (username == null) return Unauthorized();
            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return Unauthorized();

            var review = _db.Reviews.FirstOrDefault(r => r.Id == id);
            if (review == null) return NotFound();
            if (review.ReviewerId != user.Id) return Forbid();

            // allow updating rating and content
            review.Rating = dto.Rating;
            review.Content = dto.Content;
            await _db.SaveChangesAsync();

            return Ok(new OutReviewDto
            {
                Id = review.Id,
                ReviewerId = review.ReviewerId,
                ReviewerName = review.ReviewerName,
                ReviewedUserId = review.ReviewedUserId,
                ReviewedExternalId = review.ReviewedExternalId,
                Rating = review.Rating,
                Content = review.Content,
                CreatedAt = review.CreatedAt
            });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var username = User?.Identity?.Name;
            if (username == null) return Unauthorized();
            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return Unauthorized();

            var review = _db.Reviews.FirstOrDefault(r => r.Id == id);
            if (review == null) return NotFound();
            if (review.ReviewerId != user.Id) return Forbid();

            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("mine")]
        public IActionResult GetReviewsForMe()
        {
            var username = User?.Identity?.Name;
            if (username == null) return Unauthorized();
            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return Unauthorized();

            var q = _db.Reviews.Where(r => r.ReviewedUserId.HasValue && r.ReviewedUserId.Value == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new OutReviewDto
                {
                    Id = r.Id,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.ReviewerName,
                    ReviewedUserId = r.ReviewedUserId,
                    ReviewedExternalId = r.ReviewedExternalId,
                    Rating = r.Rating,
                    Content = r.Content,
                    CreatedAt = r.CreatedAt
                }).ToList();

            return Ok(q);
        }
    }
}
