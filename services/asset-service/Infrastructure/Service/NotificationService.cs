using Application.Interface;
using Application.DTOs;
using Domain.Entities;
using Infrastructure.DBs;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Service
{
    public class NotificationService : INotificationService
    {
        private readonly DBContext _db;
        private readonly UserAuthDbContext _userDb;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(
            DBContext db,
            IHubContext<NotificationHub> hub,
            UserAuthDbContext userDb)
        {
            _db = db;
            _hub = hub;
            _userDb = userDb;
        }


        public async Task<CursorResult<NotificationDto>> GetAllNotificationsCursorAsync(
    DateTime? cursor,
    int limit)
        {
            var query = _db.Notifications.AsNoTracking();

            if (cursor.HasValue)
                query = query.Where(n => n.CreatedAt < cursor.Value);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit + 1)
                .Select(n => new NotificationDto(
                    n.Id,
                    n.Title,
                    n.Text,
                    n.CreatedAt,
                    n.ExpiresAt,
                    n.Priority
                ))
                .ToListAsync();

            var hasMore = items.Count > limit;

            if (hasMore)
                items.RemoveAt(items.Count - 1);

            return new CursorResult<NotificationDto>
            {
                Items = items,
                HasMore = hasMore,
                NextCursor = items.LastOrDefault()?.CreatedAt
            };
        }


        // -----------------------------------------------------
        // CREATE NOTIFICATION FOR ALL USERS
        // -----------------------------------------------------
        public async Task<NotificationDto> CreateForUsersAsync(NotificationCreateRequest req)
        {
            var now = DateTime.UtcNow;

            var notification = new Notification
            {
                Title = req.Title,
                Text = req.Text,
                CreatedAt = now,
                ExpiresAt = req.ExpiresAt ?? now.AddDays(30),
                Priority = req.Priority
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            var userIds = await _userDb.Users
                .Select(u => u.Id.ToString())
                .ToListAsync();

            foreach (var userId in userIds)
            {
                _db.NotificationRecipients.Add(new NotificationRecipient
                {
                    NotificationId = notification.Id,
                    UserId = userId
                });

                await _hub.Clients.User(userId).SendAsync(
                    "ReceiveNotification",
                    new NotificationDto(
                        notification.Id,
                        notification.Title,
                        notification.Text,
                        notification.CreatedAt,
                        notification.ExpiresAt,
                        notification.Priority
                    )
                );
            }

            await _db.SaveChangesAsync();

            return new NotificationDto(
                notification.Id,
                notification.Title,
                notification.Text,
                notification.CreatedAt,
                notification.ExpiresAt,
                notification.Priority
            );
        }

        // -----------------------------------------------------
        // GET USER NOTIFICATIONS (CURSOR PAGINATION)
        // -----------------------------------------------------
        public async Task<CursorResult<NotificationRecipientDto>> GetForUserCursorAsync(
            string userId,
            bool unreadOnly,
            DateTime? cursor,
            int limit)
        {
            var query = _db.NotificationRecipients
                .Include(r => r.Notification)
                .Where(r => r.UserId == userId);

            if (unreadOnly)
                query = query.Where(r => !r.IsRead);

            if (cursor.HasValue)
                query = query.Where(r => r.CreatedAt < cursor.Value);

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit + 1)
                .ToListAsync();

            var hasMore = items.Count > limit;

            if (hasMore)
                items.RemoveAt(items.Count - 1);

            return new CursorResult<NotificationRecipientDto>
            {
                Items = items.Select(r => MapRecipientDto(r)).ToList(),
                HasMore = hasMore,
                NextCursor = items.LastOrDefault()?.CreatedAt
            };
        }

        // -----------------------------------------------------
        // SIMPLE USER FETCH (OPTIONAL / NON-PAGINATED)
        // -----------------------------------------------------
        public async Task<List<NotificationRecipientDto>> GetForUserAsync(string userId, bool unreadOnly)
        {
            var query = _db.NotificationRecipients
                .Include(r => r.Notification)
                .Where(r => r.UserId == userId);

            if (unreadOnly)
                query = query.Where(r => !r.IsRead);

            var list = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return list.Select(MapRecipientDto).ToList();
        }

        // -----------------------------------------------------
        // MARK AS READ
        // -----------------------------------------------------
        public async Task<bool> MarkAsReadAsync(Guid recipientId, string userId)
        {
            var rec = await _db.NotificationRecipients
                .FirstOrDefaultAsync(r => r.Id == recipientId && r.UserId == userId);

            if (rec == null) return false;

            if (!rec.IsRead)
            {
                rec.IsRead = true;
                rec.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            await _hub.Clients.User(userId)
                .SendAsync("NotificationMarkedRead", recipientId);

            return true;
        }

        // -----------------------------------------------------
        // ACKNOWLEDGE
        // -----------------------------------------------------
        public async Task<bool> AcknowledgeAsync(Guid recipientId, string userId)
        {
            var rec = await _db.NotificationRecipients
                .FirstOrDefaultAsync(r => r.Id == recipientId && r.UserId == userId);

            if (rec == null) return false;

            if (!rec.IsAcknowledged)
            {
                rec.IsAcknowledged = true;
                rec.AcknowledgedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            await _hub.Clients.User(userId)
                .SendAsync("NotificationAcknowledged", recipientId);

            return true;
        }

        // -----------------------------------------------------
        // GET ALL NOTIFICATIONS (ADMIN)
        // -----------------------------------------------------
        //public async Task<List<NotificationDto>> GetAllNotificationsAsync()
        //{
        //    return await _db.Notifications
        //        .AsNoTracking()
        //        .OrderByDescending(n => n.CreatedAt)
        //        .Select(n => new NotificationDto(
        //            n.Id,
        //            n.Title,
        //            n.Text,
        //            n.CreatedAt,
        //            n.ExpiresAt,
        //            n.Priority
        //        ))
        //        .ToListAsync();
        //}

        // -----------------------------------------------------
        // MARK ALL AS READ
        // -----------------------------------------------------
        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            var recs = await _db.NotificationRecipients
                .Where(r => r.UserId == userId && !r.IsRead)
                .ToListAsync();

            if (!recs.Any())
                return false;

            foreach (var r in recs)
            {
                r.IsRead = true;
                r.ReadAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            foreach (var r in recs)
            {
                await _hub.Clients.User(userId)
                    .SendAsync("NotificationMarkedRead", r.Id);
            }

            return true;
        }

        // -----------------------------------------------------
        // PRIVATE MAPPER (CLEAN CODE)
        // -----------------------------------------------------
        private static NotificationRecipientDto MapRecipientDto(NotificationRecipient r)
        {
            return new NotificationRecipientDto
            {
                RecipientId = r.Id,
                NotificationId = r.NotificationId,
                Title = r.Notification.Title,
                Text = r.Notification.Text,
                IsRead = r.IsRead,
                IsAcknowledged = r.IsAcknowledged,
                CreatedAt = r.CreatedAt,
                ReadAt = r.ReadAt,
                AcknowledgedAt = r.AcknowledgedAt
            };
        }
    }
}
