CREATE DATABASE [datn];
GO
USE [datn];
GO


CREATE TABLE [AuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(max) NULL,
    [UserName] nvarchar(100) NULL,
    [Action] nvarchar(50) NOT NULL,
    [EntityName] nvarchar(100) NOT NULL,
    [EntityId] nvarchar(100) NULL,
    [OldValues] nvarchar(max) NULL,
    [NewValues] nvarchar(max) NULL,
    [IpAddress] nvarchar(50) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [FeeItems] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NULL,
    [DefaultAmount] decimal(18,2) NOT NULL,
    [AgeFrom] int NULL,
    [AgeTo] int NULL,
    [IsRequired] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_FeeItems] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Holidays] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Date] date NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_Holidays] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Locations] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NULL,
    [Capacity] int NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Locations] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Menus] (
    [Id] int NOT NULL IDENTITY,
    [DayOfWeek] int NOT NULL,
    [Date] date NOT NULL,
    [MealType] int NOT NULL,
    [DishName] nvarchar(max) NOT NULL,
    [Ingredients] nvarchar(max) NULL,
    [Note] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Menus] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PayrollPeriods] (
    [Id] int NOT NULL IDENTITY,
    [Month] int NULL,
    [Year] int NULL,
    [IsLocked] bit NOT NULL,
    [LockedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PayrollPeriods] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Rankings] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NULL,
    CONSTRAINT [PK_Rankings] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Subjects] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Accounts] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(450) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [PasswordSalt] nvarchar(max) NULL,
    [Email] nvarchar(450) NOT NULL,
    [IsActive] bit NOT NULL,
    [RoleId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [PasswordResetToken] nvarchar(max) NULL,
    [ResetTokenExpires] datetime2 NULL,
    CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Accounts_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Employees] (
    [Id] int NOT NULL IDENTITY,
    [AccountId] int NOT NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NULL,
    [BaseSalary] decimal(18,2) NULL,
    [AvatarPath] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [Gender] bit NOT NULL,
    [Bio] nvarchar(max) NULL,
    [Qualifications] nvarchar(max) NULL,
    [Experience] nvarchar(max) NULL,
    [Philosophy] nvarchar(max) NULL,
    [Specialty] nvarchar(max) NULL,
    [ShowOnLanding] bit NOT NULL,
    [TeacherType] int NOT NULL,
    [SpecializedSubjects] nvarchar(max) NULL,
    [DateOfBirth] nvarchar(max) NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Employees_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [RecipientId] int NULL,
    [RecipientRole] nvarchar(max) NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Url] nvarchar(max) NULL,
    [Type] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsRead] bit NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Accounts_RecipientId] FOREIGN KEY ([RecipientId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Parents] (
    [Id] int NOT NULL IDENTITY,
    [AccountId] int NOT NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NULL,
    [Address] nvarchar(max) NULL,
    [Gender] bit NOT NULL,
    [AvatarPath] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Parents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Parents_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [AccountId] int NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [RevokedAtUtc] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Activities] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [Date] date NULL,
    [LocationId] int NULL,
    [OrganizerId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_Activities] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Activities_Employees_OrganizerId] FOREIGN KEY ([OrganizerId]) REFERENCES [Employees] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Activities_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [Classes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NULL,
    [AgeFrom] int NULL,
    [AgeTo] int NULL,
    [SchoolYear] nvarchar(max) NULL,
    [MaxCapacity] int NOT NULL,
    [IsActive] bit NOT NULL,
    [LeadTeacherId] int NULL,
    CONSTRAINT [PK_Classes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Classes_Employees_LeadTeacherId] FOREIGN KEY ([LeadTeacherId]) REFERENCES [Employees] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [EmployeeLeaveRequests] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [IsPaid] bit NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [ReviewNote] nvarchar(max) NULL,
    [ReviewedByEmployeeId] int NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ReviewedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_EmployeeLeaveRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeLeaveRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Salaries] (
    [EmployeeId] int NOT NULL,
    [PayrollPeriodId] int NOT NULL,
    [WorkingDays] decimal(18,2) NULL,
    [SalaryAmount] decimal(18,2) NULL,
    CONSTRAINT [PK_Salaries] PRIMARY KEY ([EmployeeId], [PayrollPeriodId]),
    CONSTRAINT [FK_Salaries_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Salaries_PayrollPeriods_PayrollPeriodId] FOREIGN KEY ([PayrollPeriodId]) REFERENCES [PayrollPeriods] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TeacherContracts] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [ContractNumber] nvarchar(450) NOT NULL,
    [ContractType] nvarchar(max) NOT NULL,
    [SignedDate] date NOT NULL,
    [EffectiveDate] date NOT NULL,
    [ExpiryDate] date NULL,
    [AgreedSalary] decimal(18,2) NULL,
    [WorkPosition] nvarchar(max) NULL,
    [WorkLocation] nvarchar(max) NULL,
    [WorkingHours] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [TerminationDate] date NULL,
    [TerminationReason] nvarchar(max) NULL,
    [Note] nvarchar(max) NULL,
    [OriginalFileName] nvarchar(max) NULL,
    [StoredFileName] nvarchar(max) NULL,
    [ContentType] nvarchar(max) NULL,
    [FileSize] bigint NULL,
    [UploadedAtUtc] datetime2 NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_TeacherContracts] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_TeacherContracts_ExpiryDate] CHECK ([ExpiryDate] IS NULL OR [ExpiryDate] >= [EffectiveDate]),
    CONSTRAINT [FK_TeacherContracts_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [WorkAttendances] (
    [EmployeeId] int NOT NULL,
    [Date] date NOT NULL,
    [CheckInAtUtc] datetime2 NULL,
    [CheckOutAtUtc] datetime2 NULL,
    [WorkedMinutes] int NULL,
    [WorkUnit] decimal(18,2) NULL,
    [IsLate] bit NOT NULL,
    [PenaltyAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Note] nvarchar(max) NULL,
    [ReviewedByEmployeeId] int NULL,
    [ReviewedAtUtc] datetime2 NULL,
    [ReviewNote] nvarchar(max) NULL,
    CONSTRAINT [PK_WorkAttendances] PRIMARY KEY ([EmployeeId], [Date]),
    CONSTRAINT [FK_WorkAttendances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Assignments] (
    [EmployeeId] int NOT NULL,
    [ClassId] int NOT NULL,
    [StartDate] date NOT NULL,
    [RoleInClass] nvarchar(max) NULL,
    [EndDate] date NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Assignments] PRIMARY KEY ([EmployeeId], [ClassId], [StartDate]),
    CONSTRAINT [FK_Assignments_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Assignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ClassActivities] (
    [ClassId] int NOT NULL,
    [ActivityId] int NOT NULL,
    CONSTRAINT [PK_ClassActivities] PRIMARY KEY ([ClassId], [ActivityId]),
    CONSTRAINT [FK_ClassActivities_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassActivities_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ClassCoverageBonuses] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [ClassId] int NOT NULL,
    [Date] date NOT NULL,
    [AbsentEmployeeId] int NOT NULL,
    [LeaveRequestId] int NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [Note] nvarchar(max) NULL,
    CONSTRAINT [PK_ClassCoverageBonuses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClassCoverageBonuses_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassCoverageBonuses_Employees_AbsentEmployeeId] FOREIGN KEY ([AbsentEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassCoverageBonuses_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ClassSchedules] (
    [Id] int NOT NULL IDENTITY,
    [ClassId] int NOT NULL,
    [SubjectId] int NOT NULL,
    [EmployeeId] int NULL,
    [DayOfWeek] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [LocationId] int NULL,
    [EffectiveFrom] date NOT NULL,
    [EffectiveTo] date NULL,
    [Note] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_ClassSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClassSchedules_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassSchedules_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassSchedules_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_ClassSchedules_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Students] (
    [Id] int NOT NULL IDENTITY,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [Gender] bit NOT NULL,
    [DateOfBirth] date NOT NULL,
    [Address] nvarchar(max) NULL,
    [ClassId] int NULL,
    [EnrollDate] date NULL,
    [AvatarPath] nvarchar(max) NULL,
    [Allergies] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Students] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Students_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [Substitutions] (
    [Id] int NOT NULL IDENTITY,
    [ClassScheduleId] int NOT NULL,
    [Date] date NOT NULL,
    [OriginalEmployeeId] int NOT NULL,
    [SubstituteEmployeeId] int NOT NULL,
    [Note] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_Substitutions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Substitutions_ClassSchedules_ClassScheduleId] FOREIGN KEY ([ClassScheduleId]) REFERENCES [ClassSchedules] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Substitutions_Employees_OriginalEmployeeId] FOREIGN KEY ([OriginalEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Substitutions_Employees_SubstituteEmployeeId] FOREIGN KEY ([SubstituteEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Attendances] (
    [StudentId] int NOT NULL,
    [Date] date NOT NULL,
    [Status] nvarchar(max) NULL,
    [TakenBy] int NULL,
    CONSTRAINT [PK_Attendances] PRIMARY KEY ([StudentId], [Date]),
    CONSTRAINT [FK_Attendances_Employees_TakenBy] FOREIGN KEY ([TakenBy]) REFERENCES [Employees] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Attendances_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [DailyReports] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [Date] date NOT NULL,
    [EatingStatus] int NOT NULL,
    [EatingNote] nvarchar(max) NULL,
    [SleepingStatus] int NOT NULL,
    [SleepingNote] nvarchar(max) NULL,
    [HygieneNote] nvarchar(max) NULL,
    [HealthNote] nvarchar(max) NULL,
    [ActivityNote] nvarchar(max) NULL,
    [MoodNote] nvarchar(max) NULL,
    [PhotoPaths] nvarchar(max) NULL,
    CONSTRAINT [PK_DailyReports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DailyReports_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [HealthRecords] (
    [StudentId] int NOT NULL,
    [Date] date NOT NULL,
    [Weight] decimal(18,2) NULL,
    [Height] decimal(18,2) NULL,
    [Temperature] decimal(18,2) NULL,
    [Note] nvarchar(max) NULL,
    CONSTRAINT [PK_HealthRecords] PRIMARY KEY ([StudentId], [Date]),
    CONSTRAINT [FK_HealthRecords_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ParentStudents] (
    [ParentId] int NOT NULL,
    [StudentId] int NOT NULL,
    [Relationship] nvarchar(max) NULL,
    CONSTRAINT [PK_ParentStudents] PRIMARY KEY ([ParentId], [StudentId]),
    CONSTRAINT [FK_ParentStudents_Parents_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Parents] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ParentStudents_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [StudentActivities] (
    [StudentId] int NOT NULL,
    [ActivityId] int NOT NULL,
    [Note] nvarchar(255) NULL,
    CONSTRAINT [PK_StudentActivities] PRIMARY KEY ([StudentId], [ActivityId]),
    CONSTRAINT [FK_StudentActivities_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentActivities_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [StudentFeeConfigs] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [FeeItemId] int NOT NULL,
    [CustomAmount] decimal(18,2) NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [DiscountPercentage] decimal(18,2) NOT NULL,
    [Note] nvarchar(max) NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_StudentFeeConfigs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentFeeConfigs_FeeItems_FeeItemId] FOREIGN KEY ([FeeItemId]) REFERENCES [FeeItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentFeeConfigs_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [StudyReports] (
    [StudentId] int NOT NULL,
    [Date] date NOT NULL,
    [RankingId] int NULL,
    [TeacherId] int NULL,
    [Comment] nvarchar(max) NULL,
    CONSTRAINT [PK_StudyReports] PRIMARY KEY ([StudentId], [Date]),
    CONSTRAINT [FK_StudyReports_Employees_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Employees] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_StudyReports_Rankings_RankingId] FOREIGN KEY ([RankingId]) REFERENCES [Rankings] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_StudyReports_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Tuitions] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NULL,
    [Month] int NULL,
    [Year] int NULL,
    [ExtraFee] decimal(18,2) NULL,
    [IsPaid] bit NOT NULL,
    [PaymentMethod] nvarchar(max) NULL,
    [TransactionId] nvarchar(max) NULL,
    [PaidAt] datetime2 NULL,
    [PaymentStatus] nvarchar(max) NULL,
    CONSTRAINT [PK_Tuitions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tuitions_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TuitionDetails] (
    [Id] int NOT NULL IDENTITY,
    [TuitionId] int NOT NULL,
    [FeeItemId] int NULL,
    [SubjectId] int NULL,
    [Name] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_TuitionDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TuitionDetails_FeeItems_FeeItemId] FOREIGN KEY ([FeeItemId]) REFERENCES [FeeItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TuitionDetails_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TuitionDetails_Tuitions_TuitionId] FOREIGN KEY ([TuitionId]) REFERENCES [Tuitions] ([Id]) ON DELETE CASCADE
);
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Rankings]'))
    SET IDENTITY_INSERT [Rankings] ON;
INSERT INTO [Rankings] ([Id], [Name])
VALUES (1, N'Đạt'),
(2, N'Cần cố gắng hơn');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[Rankings]'))
    SET IDENTITY_INSERT [Rankings] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [Description], [Name])
VALUES (1, N'Quản lý', N'Manager'),
(2, N'Giáo viên', N'Employee'),
(3, N'Phụ huynh', N'Parent');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;
GO


CREATE UNIQUE INDEX [IX_Accounts_Email] ON [Accounts] ([Email]);
GO


CREATE INDEX [IX_Accounts_RoleId] ON [Accounts] ([RoleId]);
GO


CREATE UNIQUE INDEX [IX_Accounts_Username] ON [Accounts] ([Username]);
GO


CREATE INDEX [IX_Activities_LocationId] ON [Activities] ([LocationId]);
GO


CREATE INDEX [IX_Activities_OrganizerId] ON [Activities] ([OrganizerId]);
GO


CREATE INDEX [IX_Assignments_ClassId] ON [Assignments] ([ClassId]);
GO


CREATE INDEX [IX_Attendances_TakenBy] ON [Attendances] ([TakenBy]);
GO


CREATE INDEX [IX_ClassActivities_ActivityId] ON [ClassActivities] ([ActivityId]);
GO


CREATE INDEX [IX_ClassCoverageBonuses_AbsentEmployeeId] ON [ClassCoverageBonuses] ([AbsentEmployeeId]);
GO


CREATE INDEX [IX_ClassCoverageBonuses_ClassId] ON [ClassCoverageBonuses] ([ClassId]);
GO


CREATE INDEX [IX_ClassCoverageBonuses_EmployeeId_ClassId_Date_AbsentEmployeeId_Status] ON [ClassCoverageBonuses] ([EmployeeId], [ClassId], [Date], [AbsentEmployeeId], [Status]);
GO


CREATE INDEX [IX_Classes_LeadTeacherId] ON [Classes] ([LeadTeacherId]);
GO


CREATE INDEX [IX_ClassSchedules_ClassId_DayOfWeek_StartTime_EndTime_EffectiveFrom] ON [ClassSchedules] ([ClassId], [DayOfWeek], [StartTime], [EndTime], [EffectiveFrom]);
GO


CREATE INDEX [IX_ClassSchedules_EmployeeId] ON [ClassSchedules] ([EmployeeId]);
GO


CREATE INDEX [IX_ClassSchedules_LocationId] ON [ClassSchedules] ([LocationId]);
GO


CREATE INDEX [IX_ClassSchedules_SubjectId] ON [ClassSchedules] ([SubjectId]);
GO


CREATE INDEX [IX_DailyReports_StudentId_Date] ON [DailyReports] ([StudentId], [Date]);
GO


CREATE INDEX [IX_EmployeeLeaveRequests_EmployeeId] ON [EmployeeLeaveRequests] ([EmployeeId]);
GO


CREATE UNIQUE INDEX [IX_Employees_AccountId] ON [Employees] ([AccountId]);
GO


CREATE UNIQUE INDEX [IX_FeeItems_Name] ON [FeeItems] ([Name]);
GO


CREATE INDEX [IX_Menus_DayOfWeek_MealType] ON [Menus] ([DayOfWeek], [MealType]);
GO


CREATE INDEX [IX_Notifications_RecipientId] ON [Notifications] ([RecipientId]);
GO


CREATE UNIQUE INDEX [IX_Parents_AccountId] ON [Parents] ([AccountId]);
GO


CREATE INDEX [IX_ParentStudents_StudentId] ON [ParentStudents] ([StudentId]);
GO


CREATE INDEX [IX_RefreshTokens_AccountId] ON [RefreshTokens] ([AccountId]);
GO


CREATE INDEX [IX_Salaries_PayrollPeriodId] ON [Salaries] ([PayrollPeriodId]);
GO


CREATE INDEX [IX_StudentActivities_ActivityId] ON [StudentActivities] ([ActivityId]);
GO


CREATE INDEX [IX_StudentFeeConfigs_FeeItemId] ON [StudentFeeConfigs] ([FeeItemId]);
GO


CREATE INDEX [IX_StudentFeeConfigs_StudentId] ON [StudentFeeConfigs] ([StudentId]);
GO


CREATE INDEX [IX_Students_ClassId] ON [Students] ([ClassId]);
GO


CREATE INDEX [IX_StudyReports_RankingId] ON [StudyReports] ([RankingId]);
GO


CREATE INDEX [IX_StudyReports_TeacherId] ON [StudyReports] ([TeacherId]);
GO


CREATE INDEX [IX_Substitutions_ClassScheduleId] ON [Substitutions] ([ClassScheduleId]);
GO


CREATE INDEX [IX_Substitutions_OriginalEmployeeId] ON [Substitutions] ([OriginalEmployeeId]);
GO


CREATE INDEX [IX_Substitutions_SubstituteEmployeeId] ON [Substitutions] ([SubstituteEmployeeId]);
GO


CREATE UNIQUE INDEX [IX_TeacherContracts_ContractNumber] ON [TeacherContracts] ([ContractNumber]);
GO


CREATE UNIQUE INDEX [IX_TeacherContracts_EmployeeId_Status] ON [TeacherContracts] ([EmployeeId], [Status]) WHERE [Status] = 1;
GO


CREATE INDEX [IX_TeacherContracts_ExpiryDate] ON [TeacherContracts] ([ExpiryDate]);
GO


CREATE INDEX [IX_TeacherContracts_Status] ON [TeacherContracts] ([Status]);
GO


CREATE INDEX [IX_TuitionDetails_FeeItemId] ON [TuitionDetails] ([FeeItemId]);
GO


CREATE INDEX [IX_TuitionDetails_SubjectId] ON [TuitionDetails] ([SubjectId]);
GO


CREATE INDEX [IX_TuitionDetails_TuitionId] ON [TuitionDetails] ([TuitionId]);
GO


CREATE UNIQUE INDEX [IX_Tuitions_StudentId_Month_Year] ON [Tuitions] ([StudentId], [Month], [Year]) WHERE [StudentId] IS NOT NULL AND [Month] IS NOT NULL AND [Year] IS NOT NULL;
GO



SET IDENTITY_INSERT [Accounts] ON;
INSERT INTO [Accounts] ([Id], [Username], [PasswordHash], [PasswordSalt], [Email], [IsActive], [RoleId], [CreatedAt], [UpdatedAt], [PasswordResetToken], [ResetTokenExpires])
VALUES
(1, N'admin', '$2a$11$in9cExoeHH9CVmTCZNfaXOB12KzbL3KE4cdB23kfI9TZe6gKrrQs.', N'', N'admin@senhong.edu.vn', 1, 1, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1001, N'phuong.ntt', '$2a$11$hkBKg1hjrJ5hp5QopglJRuoRxJkoP1S4P7L5PGXivKHcI8xWztWii', N'', N'phuong.ntt@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1002, N'chi.lnd', '$2a$11$T3/ilxXSadI2rhzIraH2Fu..Idrx9.lJOln9cUSeKlfEO321PCwhi', N'', N'chi.lnd@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1003, N'khang.tm', '$2a$11$uIF2BZHV0DIsQMZNEH0AcOOCwQduJlyMT8nG6MixLFe1K6azi1ns2', N'', N'khang.tm@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1004, N'thanh.dth', '$2a$11$gJfpSV532Y6e6X1XrPW2zuE2DRZKpDhcUIhp6Z95nfVSzHk6vnfBG', N'', N'thanh.dth@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1005, N'nam.pp', '$2a$11$7zKgfQVyEA5EAn/Nzh8bt.PMbrcrZO/LUpL3swbUknAgfYzweQKhi', N'', N'nam.pp@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1006, N'hoai.hx', '$2a$11$gmdUceHwZqnu9bMY3z7eI.uSDVuv4B92muvtb4ENOmvxxYEBdGYzW', N'', N'hoai.hx@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1007, N'hang.ntt', '$2a$11$2R/.mMpRlqj04hxAZV/Jp.38yxiaL9E.hmGLCYkE8VhuQoohaeOP6', N'', N'hang.ntt@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1008, N'thanh.kt', '$2a$11$p6dlRGHPT5KQQckO1G.Kg.LcD.p5vsYewz7VWfC5yTKV7bGyz5krW', N'', N'thanh.kt@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1009, N'lieu.tt', '$2a$11$33eWK9I8S2vuUGT.2ob9ruX.VCVqtCF1S9UnsGJJllcfyU8EjgDiy', N'', N'lieu.tt@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1010, N'van.ntb', '$2a$11$IDjsEizhnfn667r3NV0eee/h7t76mCnR3XffYjeEhz0MlmmTOQNW.', N'', N'van.ntb@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1011, N'dung.tm', '$2a$11$LnDfansNeroewHV6rfZ.vefERegBHYpOfIXly5I3kGqQuz/rgSPb.', N'', N'dung.tm@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1012, N'quan.ln', '$2a$11$cH3IHbxqa2ZSTg6qeTY7FuVux.xMC3/XrSllLvOkiCKs/Cq91mIjG', N'', N'quan.ln@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1013, N'phuong.nt', '$2a$11$0PbdIMleTaWQJFc6pkjZ/O.X1TbH8m/N6rIbsgb92bvC5/wpqcuEC', N'', N'phuong.nt@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1014, N'dung.ptk', '$2a$11$u6yd5b9lsS7pLaLO.lDtO.B/5lDXI59WUxCwggDvALpeBrxqlCmUm', N'', N'dung.ptk@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(1015, N'hoa.nv', '$2a$11$nD2JG31nZOBZskC62VdFFu4gj7BdK9RTy.ZRYXHrp59SMUB6h0PIO', N'', N'hoa.nv@senhong.edu.vn', 1, 2, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2001, N'hang.nguyen', '$2a$11$XPOsAKe.7SyK5WUR9ZNFR.kAk6g9bdGLluQCHuBYAhIRVXymVcIqO', N'', N'hang.nguyen@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2002, N'huy.tran', '$2a$11$pujbMUmGqY7wx6RAY2UOEeSSNGmzuBS882JLKsOFdvrqzxCIyvxVy', N'', N'huy.tran@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2003, N'mai.le', '$2a$11$czr5K8GJr0fPwvHMqH6SiOAZhrs6b/z08o9vIZ0l0tqGLHoIAhj4.', N'', N'mai.le@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2004, N'thang.pham', '$2a$11$ONJxkchSAdLnkM7qHH2anetVhda2CiYVZrUAowyXoNai0djyk9Cke', N'', N'thang.pham@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2005, N'duyen.ho', '$2a$11$RvmdW2RPElS4CMw.9jxBSe2YfkKMqZyw/9piZfjIxYjMRBiMEDkaK', N'', N'duyen.ho@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2006, N'quan.do', '$2a$11$W19cRurKpsZhP1FZFjbvmuwFDeS7rl73YbUZ1TKHxsUICTcLieceq', N'', N'quan.do@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2007, N'hanh.vu', '$2a$11$hhhDSi2nEioeCdvosujttu94/CxWO7v1gCZhoHdDrdNR/4SNw2fhO', N'', N'hanh.vu@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2008, N'tuan.bui', '$2a$11$6Oy/k1JUY2wkbu3ojCRGfeI45EcwL2Y4lEEkaz3T5lVyZKnoNGDti', N'', N'tuan.bui@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2009, N'ngan.dang', '$2a$11$dZcDW/7GsYivXeaPVqs26...WR8RoK9WE4GONNokJx5az7.Yn7uYS', N'', N'ngan.dang@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2010, N'phuc.ngo', '$2a$11$qI6PaQKV37joxv4EXT.ya.DCXTo.V1gzqtpisQfJoKC9quGFlWoNq', N'', N'phuc.ngo@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2011, N'lan.duong', '$2a$11$fVvLxL8TdnkZyd6lzlx08uy1OvusNqOZZxX8H/6pQ6USHXrUd6a1K', N'', N'lan.duong@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2012, N'hoa.ta', '$2a$11$lm5ORdSP.5OfMRhIBK4DGekIShaqbZ4qslbRKq6Ej0e2.OJiDMRdS', N'', N'hoa.ta@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2013, N'vy.tong', '$2a$11$0mbap1ZR5Cy6VYeibJyu7uEQtP2WbdcElUKunsVL2MAA9wlTBDb.e', N'', N'vy.tong@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2014, N'hieu.trinh', '$2a$11$rv6UgQHfqc1NupXjFlDMqeowr/7DV6WqDqOSz2/QHDMgtDs2LUwUK', N'', N'hieu.trinh@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2015, N'hue.cao', '$2a$11$2PhheGJmk9McBfOq5sLvEeX9Cc088PJuuepPx42EiLKU3on2iWIbG', N'', N'hue.cao@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2016, N'han.ly', '$2a$11$U2YpBi0n11.YL1fCWIrKlO5ucPdvIUqAPPYn2CgCQEnjrAFrTjgT.', N'', N'han.ly@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2017, N'duc.chu', '$2a$11$K60LKUaBOdfe2.QR8rc/7.wKSbNngt2wKAJUnQ4IiX7aC7pruf6.K', N'', N'duc.chu@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2018, N'anh.kieu', '$2a$11$NOQKA1s8Ej4admeJ7jpwIukKG9pEuCEXkpgs3h.ZdQSPAHTTt6Aoi', N'', N'anh.kieu@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2019, N'nam.huynh', '$2a$11$3uJZl5bIyi94.W0pYKl4P.Ayu3ZPEJnFOtTkNmBbttimR.T4EnuLq', N'', N'nam.huynh@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL),
(2020, N'linh.mai', '$2a$11$xcH3LwZpn4dePt6P4qS4nOcHW0/f5tWtCdSFKexypiBFYqYzJ8WYq', N'', N'linh.mai@dienkhanh.edu.vn', 1, 3, '2026-06-06T00:00:00', '2026-06-06T00:00:00', NULL, NULL);
SET IDENTITY_INSERT [Accounts] OFF;
GO

SET IDENTITY_INSERT [Employees] ON;
INSERT INTO [Employees] ([Id], [AccountId], [FirstName], [LastName], [Phone], [BaseSalary], [AvatarPath], [IsActive], [Gender], [Bio], [Qualifications], [Experience], [Philosophy], [Specialty], [ShowOnLanding], [TeacherType], [SpecializedSubjects])
VALUES
(1, 1, N'Hệ thống', N'Quản trị', NULL, NULL, NULL, 1, 1, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL),
(1001, 1001, N'Thụy Thanh Phương', N'Nguyễn', N'0375123456', NULL, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL),
(1002, 1002, N'Ngọc Diễm Chi', N'Lương', N'0386543210', NULL, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL),
(1003, 1003, N'Minh Khang', N'Trần', N'0386543210', NULL, NULL, 1, 1, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL),
(1004, 1004, N'Thị Hoài Thanh', N'Dương', N'0399988776', NULL, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL),
(1005, 1005, N'Phương Nam', N'Phạm', N'0377458965', NULL, NULL, 1, 1, NULL, NULL, NULL, NULL, NULL, 0, 0, NULL),
(1006, 1006, N'Xuân Hoài', N'Huỳnh', N'0371122334', NULL, NULL, 1, 1, NULL, NULL, NULL, NULL, NULL, 0, 0, NULL),
(1007, 1007, N'Thị Thúy Hằng', N'Nguyễn', N'0392233445', NULL, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, 0, 0, NULL),
(1008, 1008, N'Kim Thanh', N'Ngô', N'0385544662', NULL, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, 0, 0, NULL),
(1009, 1009, N'Thị Liễu', N'Trần', N'0399988777', NULL, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, 0, 0, NULL),
(1010, 1010, N'Thị Bảo Vân', N'Nguyễn', N'0377744110', NULL, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, 0, 0, NULL),
(1011, 1011, N'Mạnh Dũng', N'Trần', N'0392233445', NULL, NULL, 1, 1, NULL, NULL, NULL, NULL, NULL, 0, 0, NULL),
(1012, 1012, N'Nhật Quân', N'Lê', N'0392233445', NULL, NULL, 1, 1, NULL, NULL, NULL, NULL, NULL, 0, 0, NULL),
(1013, 1013, N'Thúy Phượng', N'Nguyễn', N'0378123456', NULL, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL),
(1014, 1014, N'Thị Kim Dung', N'Phan', N'0392233445', NULL, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL),
(1015, 1015, N'Vân Hoa', N'Nguyễn', N'0392233445', NULL, NULL, 1, 0, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL);
SET IDENTITY_INSERT [Employees] OFF;
GO

SET IDENTITY_INSERT [Parents] ON;
INSERT INTO [Parents] ([Id], [AccountId], [FirstName], [LastName], [Phone], [Address], [Gender], [AvatarPath], [IsActive])
VALUES
(2001, 2001, N'Thanh Hằng', N'Nguyễn', N'0912345670', NULL, 0, NULL, 1),
(2002, 2002, N'Quốc Huy', N'Trần', N'0912345671', NULL, 0, NULL, 1),
(2003, 2003, N'Thị Mai', N'Lê', N'0912345672', NULL, 0, NULL, 1),
(2004, 2004, N'Văn Thắng', N'Phạm', N'0912345673', NULL, 0, NULL, 1),
(2005, 2005, N'Mỹ Duyên', N'Hồ', N'0912345674', NULL, 0, NULL, 1),
(2006, 2006, N'Minh Quân', N'Đỗ', N'0912345675', NULL, 0, NULL, 1),
(2007, 2007, N'Thị Hạnh', N'Vũ', N'0912345676', NULL, 0, NULL, 1),
(2008, 2008, N'Anh Tuấn', N'Bùi', N'0912345677', NULL, 0, NULL, 1),
(2009, 2009, N'Kim Ngân', N'Đặng', N'0912345678', NULL, 0, NULL, 1),
(2010, 2010, N'Hoàng Phúc', N'Ngô', N'0912345679', NULL, 0, NULL, 1),
(2011, 2011, N'Thị Lan', N'Dương', N'0912345680', NULL, 0, NULL, 1),
(2012, 2012, N'Văn Hòa', N'Tạ', N'0912345681', NULL, 0, NULL, 1),
(2013, 2013, N'Thảo Vy', N'Tống', N'0912345682', NULL, 0, NULL, 1),
(2014, 2014, N'Trung Hiếu', N'Trịnh', N'0912345683', NULL, 0, NULL, 1),
(2015, 2015, N'Thị Huệ', N'Cao', N'0912345684', NULL, 0, NULL, 1),
(2016, 2016, N'Ngọc Hân', N'Lý', N'0912345685', NULL, 0, NULL, 1),
(2017, 2017, N'Minh Đức', N'Chu', N'0912345686', NULL, 0, NULL, 1),
(2018, 2018, N'Phương Anh', N'Kiều', N'0912345687', NULL, 0, NULL, 1),
(2019, 2019, N'Hải Nam', N'Huỳnh', N'0912345688', NULL, 0, NULL, 1),
(2020, 2020, N'Trúc Linh', N'Mai', N'0912345689', NULL, 0, NULL, 1);
SET IDENTITY_INSERT [Parents] OFF;
GO

