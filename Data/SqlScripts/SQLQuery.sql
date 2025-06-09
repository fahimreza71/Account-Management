Use AccMgtDB

CREATE TABLE ChartOfAccounts (
    Id INT PRIMARY KEY IDENTITY(1000,1),
    AccountName NVARCHAR(100) NOT NULL,
    ParentAccountId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

ALTER TABLE ChartOfAccounts
ADD UserId NVARCHAR(450) NOT NULL;

ALTER PROCEDURE [dbo].[sp_ManageChartOfAccounts]
    @Action NVARCHAR(20),
    @Id INT = NULL,
    @AccountName NVARCHAR(100) = NULL,
    @ParentAccountId INT = NULL,
    @IsActive BIT = 1,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = 'INSERT'
    BEGIN
        INSERT INTO ChartOfAccounts (AccountName, ParentAccountId, IsActive, UserId)
        VALUES (@AccountName, @ParentAccountId, @IsActive, @UserId)
    END

    ELSE IF @Action = 'UPDATE'
    BEGIN
        UPDATE ChartOfAccounts
        SET AccountName = @AccountName,
            ParentAccountId = @ParentAccountId,
            IsActive = @IsActive
        /* WHERE Id = @Id AND UserId = @UserId */
		WHERE Id = @Id
    END

    ELSE IF @Action = 'DELETE'
    BEGIN
        DELETE FROM ChartOfAccounts
        /* WHERE Id = @Id AND UserId = @UserId */
		WHERE Id = @Id
    END

    ELSE IF @Action = 'GET_ALL'
    BEGIN
        SELECT * FROM ChartOfAccounts
        ORDER BY ParentAccountId, AccountName
    END

    ELSE IF @Action = 'GET_BY_ID'
    BEGIN
        SELECT * FROM ChartOfAccounts
		/* WHERE Id = @Id AND UserId = @UserId */
		WHERE Id = @Id
    END
END


CREATE TABLE Vouchers (
    VoucherId INT PRIMARY KEY IDENTITY(1,1),
    VoucherType NVARCHAR(20) NOT NULL,     -- 'Journal', 'Payment' & 'Receipt'
    AccountId INT NOT NULL,                -- FK to ChartOfAccounts
    Debit DECIMAL(18,2) DEFAULT 0,
    Credit DECIMAL(18,2) DEFAULT 0,
    Narration NVARCHAR(255) NULL,
    UserId NVARCHAR(450) NOT NULL,         -- FK to AspNetUsers
    CreatedAt DATETIME DEFAULT GETDATE(),
	FOREIGN KEY (AccountId) REFERENCES ChartOfAccounts(Id),
	FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);

ALTER PROCEDURE [dbo].[sp_SaveVoucher]
    @Action NVARCHAR(50),
    @VoucherId INT = NULL,
    @VoucherType NVARCHAR(20) = NULL,
    @AccountId INT = NULL,
    @Debit DECIMAL(18,2) = 0,
    @Credit DECIMAL(18,2) = 0,
    @Narration NVARCHAR(255) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = 'INSERT'
    BEGIN
        INSERT INTO Vouchers (VoucherType, AccountId, Debit, Credit, Narration, UserId)
        VALUES (@VoucherType, @AccountId, @Debit, @Credit, @Narration, @UserId);
    END

    ELSE IF @Action = 'UPDATE'
    BEGIN
        UPDATE Vouchers
        SET
            VoucherType = @VoucherType,
            AccountId = @AccountId,
            Debit = @Debit,
            Credit = @Credit,
            Narration = @Narration
        WHERE VoucherId = @VoucherId;
    END

    ELSE IF @Action = 'DELETE'
    BEGIN
        DELETE FROM Vouchers WHERE VoucherId = @VoucherId;
    END

    ELSE IF @Action = 'GET_BY_ID'
    BEGIN
        SELECT 
            v.VoucherId,
            v.VoucherType,
            v.AccountId,
            v.Debit,
            v.Credit,
            v.Narration,
            v.UserId,
            v.CreatedAt,
            c.AccountName
        FROM Vouchers v
        INNER JOIN ChartOfAccounts c ON c.Id = v.AccountId
        WHERE v.VoucherId = @VoucherId;
    END

    ELSE IF @Action = 'GET_ALL'
	BEGIN
		SELECT 
			v.VoucherId,
			v.VoucherType,
			v.AccountId,
			c.AccountName,
			v.Debit,
			v.Credit,
			v.Narration,
			v.UserId,
			(u.FirstName + ' ' + u.LastName) AS FullName,
			v.CreatedAt
		FROM Vouchers v
		INNER JOIN ChartOfAccounts c ON c.Id = v.AccountId
		INNER JOIN AspNetUsers u ON u.Id = v.UserId
		ORDER BY v.CreatedAt DESC;
	END

	ELSE IF @Action = 'GET_BALANCE_BY_ACCOUNT'
    BEGIN
        SELECT 
			@AccountId AS AccountId,
			ISNULL(SUM(Debit), 0) - ISNULL(SUM(Credit), 0) AS Balance
		FROM Vouchers
		WHERE AccountId = @AccountId;
    END
END
