using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows.Forms;
using System.Xml.Linq;

namespace InventoryManagementApp
{
    public partial class InventoryMangementForm : Form
    {
        private string connectionString;

        private List<Product> products = new List<Product>();

        public InventoryMangementForm()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["InventoryDB"].ConnectionString;
        }

        // Adding new products to the data grid view table.
        private void btnAdd_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string insertQuery = "SET IDENTITY_INSERT Products ON;" +
                    "INSERT INTO Products (Name, Category, ItemDescription, ProductId, Quantity, Price, ImagePath, Supplier) " +
                                     "VALUES (@Name, @Category, @ItemDescription, @ProductId, @Quantity, @Price, @ImagePath, @Supplier)";
                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", txtName.Text);
                    cmd.Parameters.AddWithValue("@Category", cboxCategory.Text);
                    cmd.Parameters.AddWithValue("@Description", richTxtDescription.Text);
                    cmd.Parameters.AddWithValue("@ProductId", int.Parse(txtProductId.Text));
                    cmd.Parameters.AddWithValue("@Quantity", int.Parse(numQuantity.Text));
                    cmd.Parameters.AddWithValue("@Price", decimal.Parse(numPrice.Text));
                    cmd.Parameters.AddWithValue("@ImagePath", txtImagePath.Text);
                    cmd.Parameters.AddWithValue("@Supplier", txtSupplier.Text);
                    cmd.Parameters.AddWithValue("@DateAdded", dtpDateAdded.Value);
                    cmd.Parameters.AddWithValue("@ItemDescription", richTxtDescription.Text);
                    try
                    {
                        cmd.ExecuteNonQuery();
                        conn.Close();
                        MessageBox.Show("Product added to database successfully.");
                        LoadProducts(); // Refresh the DataGridView
                        ClearFields(); // Clear input fields
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error adding product to database: {ex.Message}");
                    }
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (dataGridViewInventory.SelectedRows.Count > 0)
            {
                int productId = Convert.ToInt32(dataGridViewInventory.SelectedRows[0].Cells["ProductID"].Value);

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "DELETE FROM Products WHERE ProductID=@ProductID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ProductID", productId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();

                    LoadProducts();
                    ClearFields();
                    MessageBox.Show("Product deleted successfully!");
                }
            }
        }

        private void btnUploadImage_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif"; // File Types for Images
            openFileDialog1.Title = "Select your product image."; // Dialog Title
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string imagePath = openFileDialog1.FileName;
                try
                {
                    picBoxProduct.Image = Image.FromFile(imagePath);
                    picBoxPreviewProduct.Image = Image.FromFile(imagePath);
                    txtImagePath.Text = imagePath;
                    MessageBox.Show("Image loaded successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}");
                }
                picBoxProduct.Tag = imagePath;

                // Associate image path with selected product
                int selectedIndex = dataGridViewInventory.SelectedRows.Count > 0 ? dataGridViewInventory.SelectedRows[0].Index : -1;
                if (selectedIndex >= 0 && selectedIndex < products.Count)
                {
                    products[selectedIndex].ImagePath = imagePath;
                }
            }
        }
        private void ClearProductDetails()
        {
            picBoxProduct.Image = null;
            picBoxPreviewProduct.Image = null;
            lblNameDetail.Text = "Name:";
            lblCategoryDetail.Text = "Category:";
            lblDescriptionDetail.Text = "Description:";
            lblPriceDetail.Text = "Price:";
            lblQuantityDetail.Text = "Quantity:";
            lblProductIDDetail.Text = "Product ID:";
        }

        // Load selected product details into input fields for editing
        private void LoadSelectedProductToInputs()
        {
            if (dataGridViewInventory.SelectedRows.Count > 0)
            {
                int index = dataGridViewInventory.SelectedRows[0].Index;
                if (index >= 0 && index < products.Count)
                {
                    Product selectedProduct = products[index];
                    txtName.Text = selectedProduct.Name;
                    cboxCategory.Text = selectedProduct.Category;
                    richTxtDescription.Text = selectedProduct.Description;
                    txtProductId.Text = selectedProduct.ProductId.ToString();
                    numQuantity.Text = selectedProduct.Quantity.ToString();
                    numPrice.Text = selectedProduct.Price.ToString();
                    picBoxProduct.Tag = selectedProduct.ImagePath;
                    if (!string.IsNullOrEmpty(selectedProduct.ImagePath) && File.Exists(selectedProduct.ImagePath))
                    {
                        picBoxProduct.Image = Image.FromFile(selectedProduct.ImagePath);
                        picBoxPreviewProduct.Image = Image.FromFile(selectedProduct.ImagePath);
                    }
                    else
                    {
                        picBoxProduct.Image = null;
                        picBoxPreviewProduct.Image = null;
                    }
                }
            }
        }

        // Edit the selected product with new values from input fields
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProductId.Text))
            {
                MessageBox.Show("Please select a product to update.");
                return;
            }
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"UPDATE Products 
                   SET Name=@Name, 
                       Category=@Category, 
                       Quantity=@Quantity, 
                       Price=@Price,
                       Supplier=@Supplier, 
                       DateAdded=@DateAdded, 
                       ImagePath=@ImagePath 
                   WHERE ProductID=@ProductID";

                SqlCommand cmd = new SqlCommand(query, conn);

                // Pass values from form controls to query
                cmd.Parameters.AddWithValue("@ProductID", Convert.ToInt32(txtProductId.Text));
                cmd.Parameters.AddWithValue("@Name", txtName.Text);
                cmd.Parameters.AddWithValue("@Category", cboxCategory.Text);
                cmd.Parameters.AddWithValue("@Quantity", numQuantity.Value);
                cmd.Parameters.AddWithValue("@Price", decimal.Parse(numPrice.Text));
                cmd.Parameters.AddWithValue("@Supplier", txtSupplier.Text);
                cmd.Parameters.AddWithValue("@DateAdded", dtpDateAdded.Value);
                cmd.Parameters.AddWithValue("@ImagePath", txtImagePath.Text);

                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                conn.Close();

                // Check if update succeeded
                if (rows > 0)
                {
                    MessageBox.Show("Product updated successfully!");

                    // Refresh DataGridView
                    LoadProducts();

                    // Clear fields after update
                    ClearFields();
                }
                else
                {
                    MessageBox.Show("Update failed. Please select a valid product.");
                }
            }

        }

        private Product? GetSelectedProduct()
        {
            if (dataGridViewInventory.SelectedRows.Count > 0)
            {
                var row = dataGridViewInventory.SelectedRows[0];
                int productId = Convert.ToInt32(row.Cells[5].Value);
                return products.FirstOrDefault(p => p.ProductId == productId);
            }
            return null;
        }

        private void LoadProducts()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Products", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dataGridViewInventory.DataSource = dt;
            }
        }
        private void ClearFields()
        {
            txtName.Clear();
            txtSupplier.Clear();
            dtpDateAdded.Value = DateTime.Now;
            cboxCategory.SelectedIndex = -1;
            richTxtDescription.Clear();
            txtProductId.Clear();
            txtImagePath.Clear();
            numQuantity.Value = 0;
            numPrice.Value = 0;
            picBoxProduct.Image = null;
            picBoxPreviewProduct.Image = null;
        }

        private void dataGridViewInventory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // ignore header row
            {
                DataGridViewRow row = dataGridViewInventory.Rows[e.RowIndex];

                // pre-load values in case of update
                txtProductId.Text = row.Cells["ProductID"].Value.ToString();
                txtName.Text = row.Cells["Name"].Value.ToString();
                cboxCategory.Text = row.Cells["Category"].Value.ToString();
                numQuantity.Value = Convert.ToDecimal(row.Cells["Quantity"].Value);
                numPrice.Text = row.Cells["Price"].Value.ToString();
                txtSupplier.Text = row.Cells["Supplier"].Value.ToString();
                dtpDateAdded.Value = Convert.ToDateTime(row.Cells["DateAdded"].Value);
                richTxtDescription.Text = row.Cells["ItemDescription"].Value.ToString();
                
                // load values to product details too
                lblProductIDDetail.Text = $"Product ID: {row.Cells["ProductID"].Value}";
                lblNameDetail.Text = $"Name: {row.Cells["Name"].Value}";
                lblCategoryDetail.Text = $"Category: {row.Cells["Category"].Value}";
                lblQuantityDetail.Text = $"Quantity: {row.Cells["Quantity"].Value}";
                lblPriceDetail.Text = $"Price: ${row.Cells["Price"].Value}";
                lblDescriptionDetail.Text = $"Description: {row.Cells["ItemDescription"].Value}";


                if (row.Cells["ImagePath"].Value != DBNull.Value)
                {
                    txtImagePath.Text = row.Cells["ImagePath"].Value.ToString();
                    picBoxProduct.ImageLocation = txtImagePath.Text;
                }
                else
                {
                    txtImagePath.Clear();
                    picBoxProduct.Image = null;
                }
            }

        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearFields();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            LoadProducts();   // Refresh DataGridView with all products
            ClearFields();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM Products WHERE Name LIKE @Search OR Category LIKE @Search OR Supplier LIKE @Search";
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                da.SelectCommand.Parameters.AddWithValue("@Search", "%" + txtSearch.Text + "%");

                DataTable dt = new DataTable();
                da.Fill(dt);
                dataGridViewInventory.DataSource = dt;
            }

        }
    }
}


