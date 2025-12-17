using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Services;
using TrainingCompany.Web.Models;
using System.Data;

namespace TrainingCompany.Services
{
    [WebService(Namespace = "http://trainingcompany.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class AdminService : System.Web.Services.WebService
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["TrainingCompanyDB"].ConnectionString;

        #region Course Management

        [WebMethod]
        public ServiceResponse AddCourse(Course course)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(course.Title))
                    return new ServiceResponse { Success = false, Message = "Course title is required." };

                if (course.Price < 0)
                    return new ServiceResponse { Success = false, Message = "Price must be a positive number." };

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"INSERT INTO Courses (Title, Description, CategoryID, Price, Schedule, Mode, Duration, ImageURL, IsActive)
                                   VALUES (@Title, @Description, @CategoryID, @Price, @Schedule, @Mode, @Duration, @ImageURL, @IsActive);
                                   SELECT CAST(SCOPE_IDENTITY() as int);";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Title", course.Title);
                    cmd.Parameters.AddWithValue("@Description", course.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CategoryID", course.CategoryID > 0 ? (object)course.CategoryID : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Price", course.Price);
                    cmd.Parameters.AddWithValue("@Schedule", course.Schedule ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Mode", course.Mode ?? "Online");
                    cmd.Parameters.AddWithValue("@Duration", course.Duration ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ImageURL", course.ImageURL ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", true);

                    conn.Open();
                    int courseId = (int)cmd.ExecuteScalar();

                    return new ServiceResponse
                    {
                        Success = true,
                        Message = "Course added successfully.",
                        Data = courseId.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        [WebMethod]
        public ServiceResponse UpdateCourse(Course course)
        {
            try
            {
                if (course.CourseID <= 0)
                    return new ServiceResponse { Success = false, Message = "Invalid Course ID." };

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"UPDATE Courses SET 
                                   Title = @Title, 
                                   Description = @Description, 
                                   CategoryID = @CategoryID, 
                                   Price = @Price, 
                                   Schedule = @Schedule, 
                                   Mode = @Mode, 
                                   Duration = @Duration,
                                   ImageURL = @ImageURL
                                   WHERE CourseID = @CourseID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@CourseID", course.CourseID);
                    cmd.Parameters.AddWithValue("@Title", course.Title);
                    cmd.Parameters.AddWithValue("@Description", course.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CategoryID", course.CategoryID > 0 ? (object)course.CategoryID : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Price", course.Price);
                    cmd.Parameters.AddWithValue("@Schedule", course.Schedule ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Mode", course.Mode ?? "Online");
                    cmd.Parameters.AddWithValue("@Duration", course.Duration ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ImageURL", course.ImageURL ?? (object)DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return new ServiceResponse { Success = true, Message = "Course updated successfully." };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        [WebMethod]
        public ServiceResponse DeleteCourse(int courseId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "UPDATE Courses SET IsActive = 0 WHERE CourseID = @CourseID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@CourseID", courseId);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return new ServiceResponse { Success = true, Message = "Course deleted successfully." };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        [WebMethod]
        public List<Course> GetAllCourses()
        {
            List<Course> courses = new List<Course>();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"SELECT c.*, cat.CategoryName 
                                   FROM Courses c 
                                   LEFT JOIN Categories cat ON c.CategoryID = cat.CategoryID 
                                   WHERE c.IsActive = 1 
                                   ORDER BY c.CreatedDate DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        courses.Add(new Course
                        {
                            CourseID = (int)reader["CourseID"],
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"].ToString(),
                            CategoryID = (int)reader["CategoryID"],
                            CategoryName = reader["CategoryName"].ToString(),
                            Price = (decimal)reader["Price"],
                            Schedule = reader["Schedule"].ToString(),
                            Mode = reader["Mode"].ToString(),
                            Duration = reader["Duration"].ToString(),
                            ImageURL = reader["ImageURL"].ToString()
                        });
                    }
                }
            }
            catch { }
            return courses;
        }

        [WebMethod]
        public Course GetCourseById(int courseId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"SELECT c.*, cat.CategoryName 
                                   FROM Courses c 
                                   LEFT JOIN Categories cat ON c.CategoryID = cat.CategoryID 
                                   WHERE c.CourseID = @CourseID AND c.IsActive = 1";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@CourseID", courseId);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        return new Course
                        {
                            CourseID = (int)reader["CourseID"],
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "",
                            CategoryID = reader["CategoryID"] != DBNull.Value ? (int)reader["CategoryID"] : 0,
                            CategoryName = reader["CategoryName"] != DBNull.Value ? reader["CategoryName"].ToString() : "",
                            Price = (decimal)reader["Price"],
                            Schedule = reader["Schedule"].ToString() ,
                            Mode = reader["Mode"].ToString() ,
                            Duration =  reader["Duration"].ToString() 
                        };
                    }
                }
            }
            catch { }
            return null;
        }

        #endregion

        #region FAQ Management

        [WebMethod]
        public ServiceResponse AddFAQ(FAQ faq)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(faq.Question) || string.IsNullOrWhiteSpace(faq.Answer))
                    return new ServiceResponse { Success = false, Message = "Question and Answer are required." };

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"INSERT INTO FAQs (Question, Answer, IntentTag, Keywords, Category, Priority, IsActive)
                                   VALUES (@Question, @Answer, @IntentTag, @Keywords, @Category, @Priority, @IsActive);
                                   SELECT CAST(SCOPE_IDENTITY() as int);";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Question", faq.Question);
                    cmd.Parameters.AddWithValue("@Answer", faq.Answer);
                    cmd.Parameters.AddWithValue("@IntentTag", faq.IntentTag ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Keywords", faq.Keywords ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Category", faq.Category ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Priority", faq.Priority);
                    cmd.Parameters.AddWithValue("@IsActive", true);

                    conn.Open();
                    int faqId = (int)cmd.ExecuteScalar();

                    return new ServiceResponse { Success = true, Message = "FAQ added successfully.", Data = faqId.ToString() };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        [WebMethod]
        public ServiceResponse UpdateFAQ(FAQ faq)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"UPDATE FAQs SET 
                                   Question = @Question, 
                                   Answer = @Answer, 
                                   IntentTag = @IntentTag, 
                                   Keywords = @Keywords,
                                   Category = @Category,
                                   Priority = @Priority
                                   WHERE FAQID = @FAQID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@FAQID", faq.FAQID);
                    cmd.Parameters.AddWithValue("@Question", faq.Question);
                    cmd.Parameters.AddWithValue("@Answer", faq.Answer);
                    cmd.Parameters.AddWithValue("@IntentTag", faq.IntentTag ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Keywords", faq.Keywords ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Category", faq.Category ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Priority", faq.Priority);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return new ServiceResponse { Success = true, Message = "FAQ updated successfully." };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        [WebMethod]
        public ServiceResponse DeleteFAQ(int faqId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "UPDATE FAQs SET IsActive = 0 WHERE FAQID = @FAQID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@FAQID", faqId);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return new ServiceResponse { Success = true, Message = "FAQ deleted successfully." };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        [WebMethod]
        public List<FAQ> GetAllFAQs()
        {
            List<FAQ> faqs = new List<FAQ>();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT * FROM FAQs WHERE IsActive = 1 ORDER BY Priority DESC, CreatedDate DESC";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        faqs.Add(new FAQ
                        {
                            FAQID = (int)reader["FAQID"],
                            Question = reader["Question"].ToString(),
                            Answer = reader["Answer"].ToString(),
                            IntentTag = reader["IntentTag"] != DBNull.Value ? reader["IntentTag"].ToString() : "",
                            Keywords = reader["Keywords"] != DBNull.Value ? reader["Keywords"].ToString() : "",
                            Category = reader["Category"] != DBNull.Value ? reader["Category"].ToString() : "",
                            Priority = reader["Priority"] != DBNull.Value ? (int)reader["Priority"] : 0
                        });
                    }
                }
            }
            catch { }
            return faqs;
        }
        [WebMethod]
        public string TestConnection()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    return "Connection successful!";
                }
            }
            catch (Exception ex)
            {
                return "Connection failed: " + ex.Message;
            }
        }


        [WebMethod]
        public List<Category> GetAllCategories()
        {
            List<Category> categories = new List<Category>();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Categories WHERE IsActive = 1 ORDER BY CategoryName";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        categories.Add(new Category
                        {
                            CategoryID = (int)reader["CategoryID"],
                            CategoryName = reader["CategoryName"].ToString(),
                            Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : ""
                        });
                    }
                }
            }
            catch { }
            return categories;
        }

        #endregion
    }

    

    
}