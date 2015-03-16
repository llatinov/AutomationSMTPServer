# AutomationSMTPServer
Simple SMTP server that can be used for email verification of automated testing. More information can be found on its <a target="_blank" href="http://automationrhapsody.com/automation-smtp-server/">home page</a>.

## About
Automation SMTP Server is simple SMTP server run as console application. It acts as SMTP server saving all email messages as EML file to disk. With build in EMLFile class you can later read those mails and verify them in your automation testing project.

## Usage
1. From your automation test project add reference to AutomationSMTPServer.exe.
2. Delete previous emails

    	private string currentDir =	
			Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
    	private string mailsDir = currentDir + "temp";
    	  
    	if (Directory.Exists(mailsDir))
    	{
    		Directory.Delete(mailsDir, true);
    	}

3. Start Automation SMTP Server as process
	
    	Process smtpServer = new Process();
    	smtpServer.StartInfo.FileName = currentDir + "AutomationSMTPServer.exe";
    	smtpServer.StartInfo.Arguments = "25";
    	smtpServer.Start();

4. Read emails
	
    	string[] files = Directory.GetFiles(mailsDir);
    	List<EMLFile> mails = new List<EMLFile>();
    	  
    	foreach (string file in files)
    	{
    		EMLFile mail = new EMLFile(file);
    		mails.Add(mail);
    		File.Delete(file);
    	}

5. Do desired verifications with EMLFile objects
6. Stop Automation SMTP Server by killing the process
	
	`smtpServer.Kill();`
7. Yes, it is that easy!
