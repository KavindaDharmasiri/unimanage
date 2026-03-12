-- SQL Script to add Department column to Courses table
-- Run this in your MySQL database

ALTER TABLE `Courses` 
ADD COLUMN `Department` varchar(100) NULL AFTER `LecturerId`;

-- Update existing courses with department from their assigned lecturer
UPDATE `Courses` c
INNER JOIN `Lecturers` l ON c.LecturerId = l.UserId
SET c.Department = l.Department
WHERE c.LecturerId IS NOT NULL;