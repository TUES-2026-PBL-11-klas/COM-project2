using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PM.Core.DTOs;
using PM.Data.Context;
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
            var reviewerId = dto.ReviewerId ?? Guid.Empty;
            if (reviewerId == Guid.Empty)
            {
                var currentUser = await ResolveCurrentUserAsync();
                if (currentUser != null)
                {
                    reviewerId = currentUser.Id;
                }
            }

            if (reviewerId == Guid.Empty)
            {
                return BadRequest("reviewerId or auth required");
            }

            if (string.IsNullOrWhiteSpace(dto.ReviewedId))
            {
                return BadRequest("reviewedId required");
            }

            var resolvedReviewedUserId = await ResolveReviewedUserIdAsync(dto.ReviewedId);
            if (resolvedReviewedUserId == reviewerId)
            {
                return BadRequest("Cannot review yourself");
            }

            var reviewExists = await _db.Reviews.AnyAsync(r =>
                r.ReviewerId == reviewerId &&
                ((resolvedReviewedUserId.HasValue && r.ReviewedUserId == resolvedReviewedUserId.Value) ||
                 (!resolvedReviewedUserId.HasValue && r.ReviewedExternalId == dto.ReviewedId)));

            if (reviewExists)
            {
                return Conflict("Review already exists");
            }

            var review = new Review
            {
                ReviewerId = reviewerId,
                ReviewerName = string.IsNullOrWhiteSpace(dto.ReviewerName) ? "Anonymous" : dto.ReviewerName,
                ReviewedUserId = resolvedReviewedUserId,
                ReviewedExternalId = resolvedReviewedUserId.HasValue ? null : dto.ReviewedId,
                Rating = dto.Rating,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReviewsFor), new { reviewedId = dto.ReviewedId }, await MapReviewAsync(review));
        }

        [AllowAnonymous]
        [HttpGet("{reviewedId}")]
        public async Task<IActionResult> GetReviewsFor(string reviewedId)
        {
            if (string.IsNullOrWhiteSpace(reviewedId))
            {
                return BadRequest();
            }

            var resolvedReviewedUserId = await ResolveReviewedUserIdAsync(reviewedId);
            var query = _db.Reviews.AsQueryable();

            query = resolvedReviewedUserId.HasValue
                ? query.Where(r => r.ReviewedUserId == resolvedReviewedUserId.Value)
                : query.Where(r => r.ReviewedExternalId == reviewedId);

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var mapped = new List<OutReviewDto>();
            foreach (var review in reviews)
            {
                mapped.Add(await MapReviewAsync(review));
            }

            return Ok(mapped);
        }

        [Authorize]
        [HttpGet("authored")]
        public async Task<IActionResult> GetAuthored()
        {
            var currentUser = await ResolveCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var reviews = await _db.Reviews
                .Where(r => r.ReviewerId == currentUser.Id || r.ReviewerName == currentUser.Username)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var mapped = new List<OutReviewDto>();
            foreach (var review in reviews)
            {
                mapped.Add(await MapReviewAsync(review));
            }

            return Ok(mapped);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(Guid id, [FromBody] CreateReviewDto dto)
        {
            var currentUser = await ResolveCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
            {
                return NotFound();
            }

            if (review.ReviewerId != currentUser.Id)
            {
                return Forbid();
            }

            review.Rating = dto.Rating;
            review.Content = dto.Content;
            await _db.SaveChangesAsync();

            return Ok(await MapReviewAsync(review));
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var currentUser = await ResolveCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
            {
                return NotFound();
            }

            if (review.ReviewerId != currentUser.Id)
            {
                return Forbid();
            }

            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetReviewsForMe()
        {
            var currentUser = await ResolveCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var reviews = await _db.Reviews
                .Where(r => r.ReviewedUserId == currentUser.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var mapped = new List<OutReviewDto>();
            foreach (var review in reviews)
            {
                mapped.Add(await MapReviewAsync(review));
            }

            return Ok(mapped);
        }

        private async Task<UserDMO?> ResolveCurrentUserAsync()
        {
            var username = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        private async Task<Guid?> ResolveReviewedUserIdAsync(string reviewedId)
        {
            if (!Guid.TryParse(reviewedId, out var parsed))
            {
                return null;
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == parsed);
            if (user != null)
            {
                return user.Id;
            }

            var mentorProfile = await _db.MentorProfiles.FirstOrDefaultAsync(mp => mp.Id == parsed);
            return mentorProfile?.UserId;
        }

        private async Task<OutReviewDto> MapReviewAsync(Review review)
        {
            return new OutReviewDto
            {
                Id = review.Id,
                ReviewerId = review.ReviewerId,
                ReviewerName = review.ReviewerName,
                ReviewedUserId = review.ReviewedUserId,
                ReviewedExternalId = review.ReviewedExternalId,
                ReviewedUserName = await ResolveReviewedUserNameAsync(review),
                Rating = review.Rating,
                Content = review.Content,
                CreatedAt = review.CreatedAt
            };
        }

        private async Task<string?> ResolveReviewedUserNameAsync(Review review)
        {
            if (review.ReviewedUserId.HasValue)
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == review.ReviewedUserId.Value);
                return user != null ? FormatDisplayName(user.Username) : null;
            }

            if (!string.IsNullOrWhiteSpace(review.ReviewedExternalId))
            {
                var user = await _db.Users.FirstOrDefaultAsync(u =>
                    u.Id.ToString() == review.ReviewedExternalId || u.Username == review.ReviewedExternalId);
                if (user != null)
                {
                    return FormatDisplayName(user.Username);
                }

                var mentorProfile = await _db.MentorProfiles
                    .Include(mp => mp.User)
                    .FirstOrDefaultAsync(mp =>
                        mp.Id.ToString() == review.ReviewedExternalId ||
                        mp.UserId.ToString() == review.ReviewedExternalId);
                if (mentorProfile?.User != null)
                {
                    return FormatDisplayName(mentorProfile.User.Username);
                }
            }

            return null;
        }

        private static string FormatDisplayName(string username)
        {
            var cleaned = System.Text.RegularExpressions.Regex.Replace(username ?? string.Empty, "[^a-zA-Z0-9]+", " ");
            var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i][1..];
                }
            }

            return parts.Length == 0 ? username ?? string.Empty : string.Join(' ', parts);
        }
    }
}
