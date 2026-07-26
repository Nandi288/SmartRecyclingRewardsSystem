DELETE FROM SystemConfigs;

INSERT INTO SystemConfigs ([Key], [Value], Description, LastUpdated)
VALUES 
('AppName', 'EcoRewards SA', 'Application display name', GETDATE()),
('AppTagline', 'Recycle. Earn. Repeat.', 'Tagline shown on landing page', GETDATE()),
('MinRedemptionPoints', '100', 'Minimum points needed to redeem a reward', GETDATE()),
('StreakBonusPoints', '20', 'Bonus points for a 5-week recycling streak', GETDATE()),
('SmtpHost', 'smtp.gmail.com', 'SMTP server host', GETDATE()),
('SmtpPort', '587', 'SMTP server port', GETDATE()),
('SmtpFromEmail', 'noreply@ecorewardssa.co.za', 'From address for outgoing emails', GETDATE()),
('ClickatellApiKey', '', 'Clickatell API key for SMS notifications', GETDATE());	