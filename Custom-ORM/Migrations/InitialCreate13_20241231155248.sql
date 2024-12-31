-- SQL Migration Script
ALTER TABLE Users ADD Id;
ALTER TABLE Users ADD Name;
ALTER TABLE Users ADD phone;
ALTER TABLE Users ADD DateOfBirth;
ALTER TABLE Users ADD product;
ALTER TABLE Products ADD Id;
ALTER TABLE Products ADD name;
ALTER TABLE Orders ADD orderId;
ALTER TABLE Orders ADD ProductId;
ALTER TABLE Orders ADD ProductName;
