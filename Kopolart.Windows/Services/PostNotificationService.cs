using Kopolart.Windows.Domain.Constants;
using Kopolart.Windows.Domain.Helpers;
using Kopolart.Windows.Domain.Models;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kopolart.Windows.Services
{
    /// <summary>
    /// Builds and shows the "new post(s)" toast notification. Extracted from the form so the running
    /// app, the headless scheduled check and the hidden test action all produce identical output.
    /// </summary>
    public class PostNotificationService
    {
        /// <summary>How many post titles are listed inside a digest notification.</summary>
        private const int MaxDigestLines = 4;

        private const string OpenInAppText = "مشاهده در برنامه";
        private const string OpenInBrowserText = "مشاهده در سایت";
        private const int MaxDescriptionLength = 120;

        /// <summary>
        /// Formats the notification text for the given posts (newest first). Safe to call without
        /// showing anything, so the tray-balloon fallback can reuse the same wording.
        /// </summary>
        public PostNotificationContent CreateContent(IReadOnlyList<RssFeedItem> posts)
        {
            if (posts == null || posts.Count == 0)
                return new PostNotificationContent();

            var newest = posts[0];

            return new PostNotificationContent
            {
                Title = BuildTitle(posts),
                Body = BuildBody(posts),
                PrimaryUrl = newest.Link ?? string.Empty
            };
        }

        /// <summary>
        /// Shows the digest toast. <paramref name="appIsRunning"/> controls how the toast is opened:
        /// inside the app when it is alive, otherwise through the default browser.
        /// Returns false when the toast API is unavailable (caller may fall back to a tray balloon).
        /// </summary>
        public bool ShowDigest(IReadOnlyList<RssFeedItem> posts, bool appIsRunning)
        {
            if (posts == null || posts.Count == 0)
                return false;

            var content = CreateContent(posts);
            if (string.IsNullOrWhiteSpace(content.PrimaryUrl))
                return false;

            try
            {
                var builder = new ToastContentBuilder()
                    .AddArgument("action", "open_post")
                    .AddArgument("url", content.PrimaryUrl)
                    .AddText(content.Title)
                    .AddText(content.Body);

                string? imageUrl = posts.Select(p => p.ImageUrl).FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
                if (!string.IsNullOrWhiteSpace(imageUrl) && Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri))
                {
                    try
                    {
                        builder.AddAppLogoOverride(imageUri, ToastGenericAppLogoCrop.Circle);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[PostNotificationService] Logo override skipped: {ex.Message}");
                    }
                }

                builder.AddButton(BuildOpenButton(posts, appIsRunning));
                builder.Show();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PostNotificationService] Toast notification failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Shows a sample digest so the layout can be verified without waiting for a real post.
        /// </summary>
        public bool ShowTest(bool appIsRunning = true)
        {
            return ShowDigest(BuildTestPosts(), appIsRunning);
        }

        private static ToastButton BuildOpenButton(IReadOnlyList<RssFeedItem> posts, bool appIsRunning)
        {
            // More than one post: the button opens the posts archive, otherwise the single post.
            string targetUrl = posts.Count > 1
                ? AppConstants.Urls.Posts
                : posts[0].Link;

            if (appIsRunning && Uri.TryCreate(targetUrl, UriKind.Absolute, out _))
            {
                return new ToastButton()
                    .SetContent(posts.Count > 1 ? "همه مطالب تازه" : OpenInAppText)
                    .AddArgument("action", "open_post")
                    .AddArgument("url", targetUrl);
            }

            // The app is not running, so activating an app argument would have nothing to handle it;
            // open the page in the default browser instead.
            var button = new ToastButton().SetContent(posts.Count > 1 ? "مشاهده همه در سایت" : OpenInBrowserText);
            if (Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri))
            {
                button.SetProtocolActivation(uri);
            }

            return button;
        }

        private static string BuildTitle(IReadOnlyList<RssFeedItem> posts)
        {
            if (posts.Count > 1)
            {
                // Persian digits, e.g. "۳ مطلب جدید در کپلآرت"
                return $"{PersianNumberHelper.ToPersianDigits(posts.Count.ToString())} مطلب جدید در کپل‌آرت";
            }

            return string.IsNullOrWhiteSpace(posts[0].Title)
                ? "مطلب جدید در کپل‌آرت"
                : posts[0].Title;
        }

        private static string BuildBody(IReadOnlyList<RssFeedItem> posts)
        {
            if (posts.Count == 1)
            {
                string description = CleanText(posts[0].Description);
                if (string.IsNullOrWhiteSpace(description))
                    return "برای مشاهده مطلب در سایت کلیک کنید.";

                return description.Length > MaxDescriptionLength
                    ? description.Substring(0, MaxDescriptionLength - 3) + "..."
                    : description;
            }

            var lines = posts
                .Take(MaxDigestLines)
                .Select(p => $"• {CleanText(p.Title)}")
                .Where(line => line.Length > 2)
                .ToList();

            int hidden = posts.Count - lines.Count;
            if (hidden > 0)
            {
                lines.Add($"و {PersianNumberHelper.ToPersianDigits(hidden.ToString())} مطلب دیگر");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string CleanText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string withoutTags = Regex.Replace(text, "<.*?>", string.Empty);
            return Regex.Replace(withoutTags, @"\s+", " ").Trim();
        }

        private static List<RssFeedItem> BuildTestPosts()
        {
            return new List<RssFeedItem>
            {
                new RssFeedItem
                {
                    Title = "عنوان نمونه مطلب جدید — اعلان آزمایشی",
                    Link = AppConstants.Urls.Posts,
                    Description = "این یک اعلان آزمایشی برای بررسی چیدمان اعلان مطالب تازه است.",
                    Guid = "test-post-1"
                },
                new RssFeedItem
                {
                    Title = "طرح رنگ تازه — نمونه دوم",
                    Link = AppConstants.Urls.Posts,
                    Guid = "test-post-2"
                },
                new RssFeedItem
                {
                    Title = "والپیپر تازه — نمونه سوم",
                    Link = AppConstants.Urls.Posts,
                    Guid = "test-post-3"
                }
            };
        }
    }
}
