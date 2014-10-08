if (Meteor.isServer) {
  Meteor.startup(function() {
    Accounts.config({
      sendVerificationEmail: true
    });

    smtp = {
      username: 'no-reply@huddlelamp.org',   // eg: server@gentlenode.com
      password: 'test1234',   // eg: 3eeP1gtizk5eziohfervU
      server:   'smtp.1und1.de',  // eg: mail.gandi.net
      port: 25
    };

    process.env.MAIL_URL = 'smtp://' + encodeURIComponent(smtp.username) + ':' + encodeURIComponent(smtp.password) + '@' + encodeURIComponent(smtp.server) + ':' + smtp.port;

    // By default, the email is sent from no-reply@meteor.com. If you wish to receive email from users asking for help with their account, be sure to set this to an email address that you can receive email at.
    Accounts.emailTemplates.from = 'HuddleOrbiter <no-reply@proxemicinteractions.org>';

    // The public name of your application. Defaults to the DNS name of the application (eg: awesome.meteor.com).
    Accounts.emailTemplates.siteName = 'Huddle Orbiter :: A web-based simulator for Huddle';

    // A Function that takes a user object and returns a String for the subject line of the email.
    Accounts.emailTemplates.verifyEmail.subject = function(user) {
      return 'Confirm Your Email Address';
    };

    // A Function that takes a user object and a url, and returns the body text for the email.
    // Note: if you need to return HTML instead, use Accounts.emailTemplates.verifyEmail.html
    Accounts.emailTemplates.verifyEmail.html = function(user, url) {
      return 'Click on the following link to verify your email address: <a href="' + url +'">Verify E-Mail</a> or copy and paste the<br /><br />' + url + '<br /><br />to your browser.';
    };
  });
}
