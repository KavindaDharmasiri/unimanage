-- SQL Script to create the Department table
-- Run this in your MySQL database

CREATE TABLE IF NOT EXISTS `Departments` (
    `DepartmentId` int(11) NOT NULL AUTO_INCREMENT,
    `DepartmentName` varchar(100) NOT NULL,
    `Status` varchar(20) NOT NULL DEFAULT 'Active',
    `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`DepartmentId`),
    UNIQUE KEY `UK_Departments_Name` (`DepartmentName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Insert some sample departments
INSERT INTO `Departments` (`DepartmentName`, `Status`, `CreatedDate`) VALUES
('Computer Science', 'Active', NOW()),
('Applied Mathematics', 'Active', NOW()),
('Fine Arts', 'On Review', NOW()),
('Molecular Biology', 'Active', NOW()),
('Ancient History', 'Inactive', NOW())
ON DUPLICATE KEY UPDATE `DepartmentName` = VALUES(`DepartmentName`);