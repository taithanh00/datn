CREATE TABLE [Classes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NULL,
    [AgeFrom] int NULL,
    [AgeTo] int NULL,
    [SchoolYear] nvarchar(max) NULL,
    [MaxCapacity] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Classes] PRIMARY KEY ([Id])
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
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Locations] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Menus] (
    [Id] int NOT NULL IDENTITY,
    [Date] date NOT NULL,
    [MealType] int NOT NULL,
    [DishName] nvarchar(max) NOT NULL,
    [Ingredients] nvarchar(max) NULL,
    [Calories] int NULL,
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
    [Code] nvarchar(450) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [FeeAmount] decimal(18,2) NULL,
    CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Students] (
    [Id] int NOT NULL IDENTITY,
    [StudentCode] nvarchar(450) NOT NULL,
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


CREATE TABLE [Curriculums] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [Content] nvarchar(max) NULL,
    [SubjectId] int NULL,
    [AgeFrom] int NULL,
    [AgeTo] int NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Curriculums] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Curriculums_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE SET NULL
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


CREATE TABLE [MenuOverrides] (
    [Id] int NOT NULL IDENTITY,
    [MenuId] int NOT NULL,
    [StudentId] int NULL,
    [ClassId] int NULL,
    [NewDishName] nvarchar(max) NOT NULL,
    [Reason] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_MenuOverrides] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuOverrides_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MenuOverrides_Menus_MenuId] FOREIGN KEY ([MenuId]) REFERENCES [Menus] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MenuOverrides_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
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


CREATE TABLE [Employees] (
    [Id] int NOT NULL IDENTITY,
    [AccountId] int NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NULL,
    [Position] nvarchar(max) NULL,
    [BaseSalary] decimal(18,2) NULL,
    [AvatarPath] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [Bio] nvarchar(max) NULL,
    [Qualifications] nvarchar(max) NULL,
    [Experience] nvarchar(max) NULL,
    [Philosophy] nvarchar(max) NULL,
    [Specialty] nvarchar(max) NULL,
    [ShowOnLanding] bit NOT NULL,
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


CREATE TABLE [TeachingPlans] (
    [ClassId] int NOT NULL,
    [CurriculumId] int NOT NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NULL,
    [Status] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TeachingPlans] PRIMARY KEY ([ClassId], [CurriculumId], [StartDate]),
    CONSTRAINT [FK_TeachingPlans_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TeachingPlans_Curriculums_CurriculumId] FOREIGN KEY ([CurriculumId]) REFERENCES [Curriculums] ([Id]) ON DELETE NO ACTION
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


CREATE TABLE [ClassSchedules] (
    [Id] int NOT NULL IDENTITY,
    [ClassId] int NOT NULL,
    [SubjectId] int NOT NULL,
    [EmployeeId] int NOT NULL,
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


CREATE TABLE [ParentStudents] (
    [ParentId] int NOT NULL,
    [StudentId] int NOT NULL,
    [Relationship] nvarchar(max) NULL,
    CONSTRAINT [PK_ParentStudents] PRIMARY KEY ([ParentId], [StudentId]),
    CONSTRAINT [FK_ParentStudents_Parents_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Parents] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ParentStudents_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
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


CREATE TABLE [StudentActivities] (
    [StudentId] int NOT NULL,
    [ActivityId] int NOT NULL,
    [Note] nvarchar(255) NULL,
    CONSTRAINT [PK_StudentActivities] PRIMARY KEY ([StudentId], [ActivityId]),
    CONSTRAINT [FK_StudentActivities_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentActivities_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
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


CREATE INDEX [IX_ClassSchedules_ClassId_DayOfWeek_StartTime_EndTime_EffectiveFrom] ON [ClassSchedules] ([ClassId], [DayOfWeek], [StartTime], [EndTime], [EffectiveFrom]);
GO


CREATE INDEX [IX_ClassSchedules_EmployeeId] ON [ClassSchedules] ([EmployeeId]);
GO


CREATE INDEX [IX_ClassSchedules_LocationId] ON [ClassSchedules] ([LocationId]);
GO


CREATE INDEX [IX_ClassSchedules_SubjectId] ON [ClassSchedules] ([SubjectId]);
GO


CREATE INDEX [IX_Curriculums_SubjectId] ON [Curriculums] ([SubjectId]);
GO


CREATE INDEX [IX_DailyReports_StudentId_Date] ON [DailyReports] ([StudentId], [Date]);
GO


CREATE INDEX [IX_EmployeeLeaveRequests_EmployeeId] ON [EmployeeLeaveRequests] ([EmployeeId]);
GO


CREATE UNIQUE INDEX [IX_Employees_AccountId] ON [Employees] ([AccountId]);
GO


CREATE UNIQUE INDEX [IX_FeeItems_Name] ON [FeeItems] ([Name]);
GO


CREATE INDEX [IX_MenuOverrides_ClassId] ON [MenuOverrides] ([ClassId]);
GO


CREATE INDEX [IX_MenuOverrides_MenuId] ON [MenuOverrides] ([MenuId]);
GO


CREATE INDEX [IX_MenuOverrides_StudentId] ON [MenuOverrides] ([StudentId]);
GO


CREATE INDEX [IX_Menus_Date_MealType] ON [Menus] ([Date], [MealType]);
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


CREATE UNIQUE INDEX [IX_Students_StudentCode] ON [Students] ([StudentCode]);
GO


CREATE INDEX [IX_StudyReports_RankingId] ON [StudyReports] ([RankingId]);
GO


CREATE INDEX [IX_StudyReports_TeacherId] ON [StudyReports] ([TeacherId]);
GO


CREATE UNIQUE INDEX [IX_Subjects_Code] ON [Subjects] ([Code]);
GO


CREATE INDEX [IX_Substitutions_ClassScheduleId] ON [Substitutions] ([ClassScheduleId]);
GO


CREATE INDEX [IX_Substitutions_OriginalEmployeeId] ON [Substitutions] ([OriginalEmployeeId]);
GO


CREATE INDEX [IX_Substitutions_SubstituteEmployeeId] ON [Substitutions] ([SubstituteEmployeeId]);
GO


CREATE INDEX [IX_TeachingPlans_CurriculumId] ON [TeachingPlans] ([CurriculumId]);
GO


CREATE INDEX [IX_TuitionDetails_FeeItemId] ON [TuitionDetails] ([FeeItemId]);
GO


CREATE INDEX [IX_TuitionDetails_SubjectId] ON [TuitionDetails] ([SubjectId]);
GO


CREATE INDEX [IX_TuitionDetails_TuitionId] ON [TuitionDetails] ([TuitionId]);
GO


CREATE UNIQUE INDEX [IX_Tuitions_StudentId_Month_Year] ON [Tuitions] ([StudentId], [Month], [Year]) WHERE [StudentId] IS NOT NULL AND [Month] IS NOT NULL AND [Year] IS NOT NULL;
GO


