using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Services;
using TrainingCompany.Web.Models;

namespace TrainingCompany.Services
{
    [WebService(Namespace = "http://trainingcompany.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class UserService : System.Web.Services.WebService
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["TrainingCompanyDB"].ConnectionString;

        #region Authentication

        [WebMethod]
        public UserResponse Register(UserRegistration user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.Name) || string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Password))
                    return new UserResponse { Success = false, Message = "Name, Email, and Password are required." };

                if (user.Password.Length < 6)
                    return new UserResponse { Success = false, Message = "Password must be at least 6 characters." };

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    // Check if email exists
                    string checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                    SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@Email", user.Email);

                    conn.Open();
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                        return new UserResponse { Success = false, Message = "Email already registered." };

                    // Insert new user
                    string query = @"INSERT INTO Users (Name, Email, Phone, Password, IsAdmin, IsActive)
                                   VALUES (@Name, @Email, @Phone, @Password, 0, 1);
                                   SELECT CAST(SCOPE_IDENTITY() as int);";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Name", user.Name);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@Phone", user.Phone ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Password", user.Password); // In production, hash this!

                    int userId = (int)cmd.ExecuteScalar();

                    return new UserResponse
                    {
                        Success = true,
                        Message = "Registration successful!",
                        User = new User
                        {
                            UserID = userId,
                            Name = user.Name,
                            Email = user.Email,
                            IsAdmin = false
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new UserResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        [WebMethod]
        public UserResponse Login(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return new UserResponse { Success = false, Message = "Email and Password are required." };

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"SELECT UserID, Name, Email, Phone, IsAdmin 
                           FROM Users 
                           WHERE Email = @Email AND Password = @Password AND IsActive = 1";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);

                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        // 1. EXTRACT ALL DATA FIRST while the reader is open
                        int userId = (int)reader["UserID"];
                        string name = reader["Name"].ToString();
                        string userEmail = reader["Email"].ToString();
                        string phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : "";
                        bool isAdmin = (bool)reader["IsAdmin"];

                        // 2. NOW close the reader safely
                        reader.Close();

                        // 3. Update last login
                        string updateQuery = "UPDATE Users SET LastLoginDate = GETDATE() WHERE UserID = @UserID";
                        SqlCommand updateCmd = new SqlCommand(updateQuery, conn);
                        updateCmd.Parameters.AddWithValue("@UserID", userId);
                        updateCmd.ExecuteNonQuery();

                        // 4. Return the data using the variables we saved, NOT the closed reader
                        return new UserResponse
                        {
                            Success = true,
                            Message = "Login successful!",
                            User = new User
                            {
                                UserID = userId,
                                Name = name,
                                Email = userEmail,
                                Phone = phone,
                                IsAdmin = isAdmin
                            }
                        };
                    }

                    return new UserResponse { Success = false, Message = "Invalid email or password." };
                }
            }
            catch (Exception ex)
            {
                return new UserResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        #endregion

        #region Course Discovery

        [WebMethod]
        public List<CourseItem> SearchCourses(string keyword, int categoryId, string mode)
        {
            List<CourseItem> courses = new List<CourseItem>();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"SELECT c.*, cat.CategoryName 
                                   FROM Courses c 
                                   LEFT JOIN Categories cat ON c.CategoryID = cat.CategoryID 
                                   WHERE c.IsActive = 1";

                    if (!string.IsNullOrWhiteSpace(keyword))
                        query += " AND (c.Title LIKE @Keyword OR c.Description LIKE @Keyword)";

                    if (categoryId > 0)
                        query += " AND c.CategoryID = @CategoryID";

                    if (!string.IsNullOrWhiteSpace(mode))
                        query += " AND c.Mode = @Mode";

                    query += " ORDER BY c.CreatedDate DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);

                    if (!string.IsNullOrWhiteSpace(keyword))
                        cmd.Parameters.AddWithValue("@Keyword", "%" + keyword + "%");
                    if (categoryId > 0)
                        cmd.Parameters.AddWithValue("@CategoryID", categoryId);
                    if (!string.IsNullOrWhiteSpace(mode))
                        cmd.Parameters.AddWithValue("@Mode", mode);

                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        courses.Add(new CourseItem
                        {
                            CourseID = (int)reader["CourseID"],
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "",
                            CategoryName = reader["CategoryName"] != DBNull.Value ? reader["CategoryName"].ToString() : "",
                            Price = (decimal)reader["Price"],
                            Schedule = reader["Schedule"] != DBNull.Value ? reader["Schedule"].ToString() : "",
                            Mode = reader["Mode"] != DBNull.Value ? reader["Mode"].ToString() : "",
                            Duration = reader["Duration"] != DBNull.Value ? reader["Duration"].ToString() : ""
                        });
                    }
                }
            }
            catch { }
            return courses;
        }

        [WebMethod]
        public CourseItem GetCourseDetails(int courseId)
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
                        return new CourseItem
                        {
                            CourseID = (int)reader["CourseID"],
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "",
                            CategoryName = reader["CategoryName"] != DBNull.Value ? reader["CategoryName"].ToString() : "",
                            Price = (decimal)reader["Price"],
                            Schedule = reader["Schedule"] != DBNull.Value ? reader["Schedule"].ToString() : "",
                            Mode = reader["Mode"] != DBNull.Value ? reader["Mode"].ToString() : "",
                            Duration = reader["Duration"] != DBNull.Value ? reader["Duration"].ToString() : ""
                        };
                    }
                }
            }
            catch { }
            return null;
        }

        #endregion

        #region Cart Management

        [WebMethod]
        public ServiceResponse AddToCart(int userId, int courseId, int quantity)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Check if already in cart
                    string checkQuery = "SELECT Quantity FROM Cart WHERE UserID = @UserID AND CourseID = @CourseID";
                    SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@UserID", userId);
                    checkCmd.Parameters.AddWithValue("@CourseID", courseId);

                    object result = checkCmd.ExecuteScalar();

                    if (result != null)
                    {
                        // Update quantity
                        int currentQty = (int)result;
                        string updateQuery = "UPDATE Cart SET Quantity = @Quantity WHERE UserID = @UserID AND CourseID = @CourseID";
                        SqlCommand updateCmd = new SqlCommand(updateQuery, conn);
                        updateCmd.Parameters.AddWithValue("@Quantity", currentQty + quantity);
                        updateCmd.Parameters.AddWithValue("@UserID", userId);
                        updateCmd.Parameters.AddWithValue("@CourseID", courseId);
                        updateCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // Insert new
                        string insertQuery = "INSERT INTO Cart (UserID, CourseID, Quantity) VALUES (@UserID, @CourseID, @Quantity)";
                        SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
                        insertCmd.Parameters.AddWithValue("@UserID", userId);
                        insertCmd.Parameters.AddWithValue("@CourseID", courseId);
                        insertCmd.Parameters.AddWithValue("@Quantity", quantity);
                        insertCmd.ExecuteNonQuery();
                    }

                    return new ServiceResponse { Success = true, Message = "Added to cart successfully!" };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        [WebMethod]
        public List<CartItem> GetUserCart(int userId)
        {
            List<CartItem> cartItems = new List<CartItem>();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"SELECT c.CartID, c.UserID, c.CourseID, c.Quantity, c.AddedDate,
                                   co.Title, co.Description, co.Price, co.Mode, 
                                   (co.Price * c.Quantity) AS Subtotal
                                   FROM Cart c
                                   INNER JOIN Courses co ON c.CourseID = co.CourseID
                                   WHERE c.UserID = @UserID AND co.IsActive = 1";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        cartItems.Add(new CartItem
                        {
                            CartID = (int)reader["CartID"],
                            CourseID = (int)reader["CourseID"],
                            Title = reader["Title"].ToString(),
                            Price = (decimal)reader["Price"],
                            Quantity = (int)reader["Quantity"],
                            Subtotal = (decimal)reader["Subtotal"]
                        });
                    }
                }
            }
            catch { }
            return cartItems;
        }

        [WebMethod]
        public ServiceResponse UpdateCartItem(int cartId, int quantity)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "UPDATE Cart SET Quantity = @Quantity WHERE CartID = @CartID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@CartID", cartId);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return new ServiceResponse { Success = true, Message = "Cart updated successfully!" };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        [WebMethod]
        public ServiceResponse RemoveFromCart(int cartId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "DELETE FROM Cart WHERE CartID = @CartID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@CartID", cartId);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return new ServiceResponse { Success = true, Message = "Item removed from cart!" };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        [WebMethod]
        public ServiceResponse EnrollCourse(int userId, int courseId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Get course price
                    string priceQuery = "SELECT Price FROM Courses WHERE CourseID = @CourseID";
                    SqlCommand priceCmd = new SqlCommand(priceQuery, conn);
                    priceCmd.Parameters.AddWithValue("@CourseID", courseId);
                    decimal price = (decimal)priceCmd.ExecuteScalar();

                    // Create enrollment
                    string enrollQuery = @"INSERT INTO Enrollments (UserID, CourseID, Status, PaymentStatus, Amount)
                                         VALUES (@UserID, @CourseID, 'Enrolled', 'Pending', @Amount)";
                    SqlCommand enrollCmd = new SqlCommand(enrollQuery, conn);
                    enrollCmd.Parameters.AddWithValue("@UserID", userId);
                    enrollCmd.Parameters.AddWithValue("@CourseID", courseId);
                    enrollCmd.Parameters.AddWithValue("@Amount", price);
                    enrollCmd.ExecuteNonQuery();

                    // Create notification
                    string notifQuery = @"INSERT INTO Notifications (UserID, Title, Message, Type)
                                        VALUES (@UserID, 'Enrollment Successful', 
                                        'You have been enrolled in a new course. Payment pending.', 'Course')";
                    SqlCommand notifCmd = new SqlCommand(notifQuery, conn);
                    notifCmd.Parameters.AddWithValue("@UserID", userId);
                    notifCmd.ExecuteNonQuery();

                    return new ServiceResponse { Success = true, Message = "Enrolled successfully!" };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        #endregion

        #region Notifications

        [WebMethod]
        public List<Notification> GetUserNotifications(int userId, bool unreadOnly)
        {
            List<Notification> notifications = new List<Notification>();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Notifications WHERE UserID = @UserID";
                    if (unreadOnly)
                        query += " AND IsRead = 0";
                    query += " ORDER BY CreatedDate DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        notifications.Add(new Notification
                        {
                            NotificationID = (int)reader["NotificationID"],
                            Title = reader["Title"].ToString(),
                            Message = reader["Message"].ToString(),
                            Type = reader["Type"] != DBNull.Value ? reader["Type"].ToString() : "",
                            IsRead = (bool)reader["IsRead"],
                            CreatedDate = (DateTime)reader["CreatedDate"]
                        });
                    }
                }
            }
            catch { }
            return notifications;
        }

        [WebMethod]
        public ServiceResponse MarkNotificationAsRead(int notificationId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "UPDATE Notifications SET IsRead = 1, ReadDate = GETDATE() WHERE NotificationID = @NotificationID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@NotificationID", notificationId);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    return new ServiceResponse { Success = true, Message = "Marked as read." };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = "Error: " + ex.Message };
            }
        }

        #endregion
    }

    
}