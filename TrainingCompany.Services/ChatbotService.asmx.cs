using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Services;
using TrainingCompany.Web.Models;


namespace TrainingCompany.Services
{
    [WebService(Namespace = "http://trainingcompany.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class ChatbotService : System.Web.Services.WebService
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["TrainingCompanyDB"].ConnectionString;

        [WebMethod]
        public ChatResponse ProcessMessage(string userMessage, int userId, string sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userMessage))
                    return new ChatResponse { Success = false, Response = "Please enter a message." };

                // Normalize message
                string normalizedMessage = userMessage.ToLower().Trim();

                // Try to match with FAQs
                ChatResponse response = MatchFAQ(normalizedMessage);

                // If no FAQ match, try intent detection
                if (response == null)
                    response = DetectIntent(normalizedMessage);

                // Default response if nothing matched
                if (response == null)
                {
                    response = new ChatResponse
                    {
                        Success = true,
                        Response = "I'm not sure I understand. Could you rephrase that? You can ask me about courses, enrollment, pricing, schedules, or anything else!",
                        Intent = "unknown",
                        Confidence = 0
                    };
                }

                // Log chat history
                LogChatHistory(userId, sessionId, userMessage, response.Response, response.Intent, response.Confidence);

                return response;
            }
            catch (Exception ex)
            {
                return new ChatResponse
                {
                    Success = false,
                    Response = "Sorry, I encountered an error. Please try again.",
                    Intent = "error"
                };
            }
        }

        private ChatResponse MatchFAQ(string message)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT * FROM FAQs WHERE IsActive = 1 ORDER BY Priority DESC";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    List<FAQ> faqs = new List<FAQ>();
                    while (reader.Read())
                    {
                        faqs.Add(new FAQ
                        {
                            FAQID = (int)reader["FAQID"],
                            Question = reader["Question"].ToString().ToLower(),
                            Answer = reader["Answer"].ToString(),
                            IntentTag = reader["IntentTag"] != DBNull.Value ? reader["IntentTag"].ToString() : "",
                            Keywords = reader["Keywords"] != DBNull.Value ? reader["Keywords"].ToString() : "",
                            Priority = reader["Priority"] != DBNull.Value ? (int)reader["Priority"] : 0
                        });
                    }

                    // Match patterns
                    foreach (var faq in faqs)
                    {
                        string[] patterns = faq.Question.Split('|');
                        foreach (var pattern in patterns)
                        {
                            if (message.Contains(pattern.Trim()))
                            {
                                return new ChatResponse
                                {
                                    Success = true,
                                    Response = faq.Answer,
                                    Intent = faq.IntentTag,
                                    Confidence = 0.85m
                                };
                            }
                        }

                        // Keyword matching
                        if (!string.IsNullOrWhiteSpace(faq.Keywords))
                        {
                            string[] keywords = faq.Keywords.ToLower().Split(',');
                            int matchCount = keywords.Count(k => message.Contains(k.Trim()));
                            if (matchCount >= 2)
                            {
                                return new ChatResponse
                                {
                                    Success = true,
                                    Response = faq.Answer,
                                    Intent = faq.IntentTag,
                                    Confidence = 0.7m
                                };
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private ChatResponse DetectIntent(string message)
        {
            // Course search intent
            if (message.Contains("course") || message.Contains("class") || message.Contains("training"))
            {
                if (message.Contains("search") || message.Contains("find") || message.Contains("show") || message.Contains("available"))
                {
                    return new ChatResponse
                    {
                        Success = true,
                        Response = "I can help you find courses! You can browse by category like Programming, Data Science, Business, Design, or Marketing. What interests you?",
                        Intent = "course_search",
                        Confidence = 0.8m,
                        ActionType = "search_courses"
                    };
                }
            }

            // Enrollment intent
            if ((message.Contains("enroll") || message.Contains("register") || message.Contains("sign up") || message.Contains("join")) &&
                (message.Contains("course") || message.Contains("class")))
            {
                return new ChatResponse
                {
                    Success = true,
                    Response = "To enroll in a course, browse our catalog, select a course, and click 'Add to Cart'. Then proceed to checkout. Need help finding a specific course?",
                    Intent = "enrollment",
                    Confidence = 0.8m,
                    ActionType = "show_enrollment"
                };
            }

            // Pricing intent
            if (message.Contains("price") || message.Contains("cost") || message.Contains("fee") || message.Contains("pay"))
            {
                return new ChatResponse
                {
                    Success = true,
                    Response = "Our courses range from $399 to $699 depending on duration and content. Each course page displays the exact price. Would you like to see all courses with their prices?",
                    Intent = "pricing",
                    Confidence = 0.75m
                };
            }

            // Schedule intent
            if (message.Contains("schedule") || message.Contains("when") || message.Contains("time") || message.Contains("timing"))
            {
                return new ChatResponse
                {
                    Success = true,
                    Response = "We offer flexible schedules including weekday evenings, weekends, and intensive programs. Each course has specific timings. Would you like to search by your preferred schedule?",
                    Intent = "schedule",
                    Confidence = 0.75m
                };
            }

            // Cart intent
            if (message.Contains("cart") || message.Contains("basket"))
            {
                return new ChatResponse
                {
                    Success = true,
                    Response = "You can view and manage your cart from the cart icon in the navigation. Add courses, update quantities, or proceed to checkout!",
                    Intent = "cart",
                    Confidence = 0.8m,
                    ActionType = "view_cart"
                };
            }

            return null;
        }

        private void LogChatHistory(int userId, string sessionId, string userMessage, string botResponse, string intent, decimal confidence)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"INSERT INTO ChatHistory (UserID, SessionID, UserMessage, BotResponse, IntentDetected, Confidence)
                                   VALUES (@UserID, @SessionID, @UserMessage, @BotResponse, @Intent, @Confidence)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UserID", userId > 0 ? (object)userId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@SessionID", sessionId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserMessage", userMessage);
                    cmd.Parameters.AddWithValue("@BotResponse", botResponse);
                    cmd.Parameters.AddWithValue("@Intent", intent ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Confidence", confidence);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        [WebMethod]
        public List<string> GetQuickReplies()
        {
            return new List<string>
            {
                "Show me available courses",
                "How do I enroll?",
                "What are your prices?",
                "Tell me about schedules",
                "Online or on-site?",
                "Do you offer certificates?",
                "How can I contact support?"
            };
        }

        [WebMethod]
        public List<CourseRecommendation> GetCourseRecommendations(int userId)
        {
            List<CourseRecommendation> recommendations = new List<CourseRecommendation>();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    // Get top 3 popular courses
                    string query = @"SELECT TOP 3 c.CourseID, c.Title, c.Description, c.Price, c.Mode,
                                   COUNT(e.EnrollmentID) as EnrollmentCount
                                   FROM Courses c
                                   LEFT JOIN Enrollments e ON c.CourseID = e.CourseID
                                   WHERE c.IsActive = 1
                                   GROUP BY c.CourseID, c.Title, c.Description, c.Price, c.Mode
                                   ORDER BY EnrollmentCount DESC, c.CreatedDate DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        recommendations.Add(new CourseRecommendation
                        {
                            CourseID = (int)reader["CourseID"],
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "",
                            Price = (decimal)reader["Price"],
                            Mode = reader["Mode"] != DBNull.Value ? reader["Mode"].ToString() : ""
                        });
                    }
                }
            }
            catch { }
            return recommendations;
        }
    }

    
}