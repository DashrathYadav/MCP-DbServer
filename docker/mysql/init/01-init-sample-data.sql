-- MySQL initialization script for MCP Database Server
-- This script creates sample tables for testing and demonstration

USE app_db;

-- Create users table
CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    first_name VARCHAR(50),
    last_name VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE
);

-- Create products table
CREATE TABLE IF NOT EXISTS products (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    price DECIMAL(10,2) NOT NULL,
    stock_quantity INT DEFAULT 0,
    category_id INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Create orders table
CREATE TABLE IF NOT EXISTS orders (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    order_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    total_amount DECIMAL(12,2) NOT NULL,
    status ENUM('pending', 'processing', 'shipped', 'delivered', 'cancelled') DEFAULT 'pending',
    shipping_address TEXT,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Create order_items table
CREATE TABLE IF NOT EXISTS order_items (
    id INT AUTO_INCREMENT PRIMARY KEY,
    order_id INT NOT NULL,
    product_id INT NOT NULL,
    quantity INT NOT NULL,
    unit_price DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE,
    FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE
);

-- Insert sample data
INSERT IGNORE INTO users (username, email, first_name, last_name) VALUES
    ('john_doe', 'john.doe@example.com', 'John', 'Doe'),
    ('jane_smith', 'jane.smith@example.com', 'Jane', 'Smith'),
    ('bob_wilson', 'bob.wilson@example.com', 'Bob', 'Wilson'),
    ('alice_brown', 'alice.brown@example.com', 'Alice', 'Brown'),
    ('charlie_davis', 'charlie.davis@example.com', 'Charlie', 'Davis');

INSERT IGNORE INTO products (name, description, price, stock_quantity, category_id) VALUES
    ('Laptop Pro', 'High-performance laptop for professionals', 1299.99, 50, 1),
    ('Wireless Mouse', 'Ergonomic wireless mouse with precision tracking', 29.99, 200, 1),
    ('Mechanical Keyboard', 'RGB mechanical keyboard for gaming and typing', 89.99, 75, 1),
    ('USB-C Hub', '7-in-1 USB-C hub with multiple ports', 49.99, 120, 1),
    ('Monitor Stand', 'Adjustable monitor stand with storage', 39.99, 80, 1);

-- Insert sample orders (only if users exist)
INSERT IGNORE INTO orders (user_id, total_amount, status, shipping_address) VALUES
    (1, 1389.97, 'delivered', '123 Main St, City, State 12345'),
    (2, 119.98, 'shipped', '456 Oak Ave, City, State 12346'),
    (3, 49.99, 'processing', '789 Pine Rd, City, State 12347'),
    (4, 69.98, 'pending', '321 Elm St, City, State 12348');

-- Insert sample order items (only if orders exist)
INSERT IGNORE INTO order_items (order_id, product_id, quantity, unit_price) VALUES
    (1, 1, 1, 1299.99),
    (1, 2, 3, 29.99),
    (2, 3, 1, 89.99),
    (2, 2, 1, 29.99),
    (3, 4, 1, 49.99),
    (4, 5, 1, 39.99),
    (4, 2, 1, 29.99);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_orders_user_id ON orders(user_id);
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);
CREATE INDEX IF NOT EXISTS idx_order_items_order_id ON order_items(order_id);
CREATE INDEX IF NOT EXISTS idx_order_items_product_id ON order_items(product_id);

-- Show table summary
SELECT 
    'Database initialized successfully' as status,
    (SELECT COUNT(*) FROM users) as total_users,
    (SELECT COUNT(*) FROM products) as total_products,
    (SELECT COUNT(*) FROM orders) as total_orders,
    (SELECT COUNT(*) FROM order_items) as total_order_items;
