# -*- coding: utf-8 -*-
"""
This script is an automated agent that scans technology news websites,
filters them for new web browser launches, and sends an email notification.
"""

# --- CONFIGURATION ---
# To use this script, you need to provide your email credentials.

# Email Configuration
# 1. SENDER_EMAIL: Your full Gmail address (e.g., "your_email@gmail.com").
# 2. SENDER_APP_PASSWORD: A Google App Password for your account.
#    To generate an App Password:
#    - Go to your Google Account settings: https://myaccount.google.com/
#    - Navigate to "Security".
#    - Under "Signing in to Google," select "2-Step Verification" and enable it.
#    - Return to the Security page, select "App passwords".
#    - Generate a new password for "Mail" on your "Windows Computer" (or other device).
#    - Copy the 16-character password and paste it below.
# 3. RECIPIENT_EMAIL: The email address where you want to receive notifications.

SENDER_EMAIL = "your_email@gmail.com"
SENDER_APP_PASSWORD = "your_app_password"  # Use the 16-character App Password
RECIPIENT_EMAIL = "recipient_email@example.com"

# --- DO NOT EDIT BELOW THIS LINE ---

import requests
from bs4 import BeautifulSoup
import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from datetime import datetime

# --- Constants ---
WEBSITES = [
    "https://techcrunch.com/",
    "https://www.theverge.com/",
    "https://arstechnica.com/",
    "https://www.wired.com/",
    "https://www.engadget.com/",
    "https://gizmodo.com/",
    "https://www.cnet.com/tech/",
    "https://thenextweb.com/"
]
SENT_LINKS_FILE = "sent_links.txt"
LOG_FILE = "agent_log.txt"

# --- Logging Function ---
def log_activity(message):
    """
    Appends a message with a timestamp to the log file.
    """
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    with open(LOG_FILE, "a") as f:
        f.write(f"[{timestamp}] {message}\n")

# --- Scraping Functions ---

def fetch_html(url):
    """
    Fetches the HTML content of a URL with a standard User-Agent header.
    Returns the HTML content as a string, or None if an error occurs.
    """
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36"
    }
    try:
        print(f"Fetching HTML from {url}...")
        response = requests.get(url, headers=headers, timeout=15)
        response.raise_for_status()
        return response.text
    except requests.exceptions.RequestException as e:
        print(f"Error fetching {url}: {e}")
        return None

def scrape_techcrunch(html):
    """
    Scrapes headlines and links from TechCrunch's HTML content.
    """
    articles = []
    soup = BeautifulSoup(html, "html.parser")
    # Updated selector: Look for links within <h2> tags, a common pattern for headlines.
    for h2 in soup.find_all("h2"):
        link = h2.find("a")
        if link and link.has_attr("href"):
            title = link.get_text(strip=True)
            url = link.get("href")
            if title and url:
                # Ensure the URL is absolute, just in case
                if not url.startswith("http"):
                    url = "https://techcrunch.com" + url
                articles.append({"title": title, "url": url})
    print(f"Scraped {len(articles)} articles from TechCrunch.")
    return articles

def scrape_theverge(html):
    """
    Scrapes headlines and links from The Verge's HTML content.
    """
    articles = []
    soup = BeautifulSoup(html, "html.parser")
    # The Verge often has headlines in <h2> tags with a link inside
    # This is a general selector and might need adjustment if the site structure changes.
    for h2 in soup.find_all("h2"):
        link = h2.find("a")
        if link and link.has_attr("href"):
            title = link.get_text(strip=True)
            url = link["href"]
            if title and url:
                # Ensure the URL is absolute
                if not url.startswith("http"):
                    url = "https://www.theverge.com" + url
                articles.append({"title": title, "url": url})
    print(f"Scraped {len(articles)} articles from The Verge.")
    return articles

def scrape_arstechnica(html):
    """
    Scrapes headlines and links from Ars Technica's HTML content.
    """
    articles = []
    soup = BeautifulSoup(html, "html.parser")
    # Ars Technica often lists articles in <li> elements with a link inside
    for li in soup.find_all("li"):
        link = li.find("a")
        if link and link.has_attr("href"):
            title = link.get_text(strip=True)
            url = link["href"]
            if title and url:
                # Ensure the URL is absolute
                if not url.startswith("http"):
                    url = "https://arstechnica.com" + url
                articles.append({"title": title, "url": url})
    print(f"Scraped {len(articles)} articles from Ars Technica.")
    return articles

def scrape_wired(html):
    """
    Scrapes headlines and links from Wired's HTML content.
    """
    articles = []
    soup = BeautifulSoup(html, "html.parser")
    # Wired often uses <a> tags inside <h2> elements for headlines
    for h2 in soup.find_all("h2"):
        link = h2.find("a")
        if link and link.has_attr("href"):
            title = link.get_text(strip=True)
            url = link["href"]
            if title and url:
                # Ensure the URL is absolute
                if not url.startswith("http"):
                    url = "https://www.wired.com" + url
                articles.append({"title": title, "url": url})
    print(f"Scraped {len(articles)} articles from Wired.")
    return articles

def scrape_engadget(html):
    """
    Scrapes headlines and links from Engadget's HTML content.
    """
    articles = []
    soup = BeautifulSoup(html, "html.parser")
    # Updated Selector: Trying <h3> tags as they are also common for article headlines.
    for h3 in soup.find_all("h3"):
        link = h3.find("a")
        if link and link.has_attr("href"):
            title = link.get_text(strip=True)
            url = link["href"]
            if title and url:
                # Engadget links are often relative, so we need to build the full URL
                if not url.startswith("http"):
                    url = "https://www.engadget.com" + url
                articles.append({"title": title, "url": url})
    print(f"Scraped {len(articles)} articles from Engadget.")
    return articles

def scrape_gizmodo(html):
    """
    Scrapes headlines and links from Gizmodo's HTML content.
    """
    articles = []
    soup = BeautifulSoup(html, "html.parser")
    # Gizmodo often uses <h2> tags for headlines.
    for h2 in soup.find_all("h2"):
        link = h2.find("a")
        if link and link.has_attr("href"):
            title = link.get_text(strip=True)
            url = link["href"]
            if title and url:
                # Ensure the URL is absolute
                if not url.startswith("http"):
                    url = "https://gizmodo.com" + url
                articles.append({"title": title, "url": url})
    print(f"Scraped {len(articles)} articles from Gizmodo.")
    return articles

def scrape_cnet(html):
    """
    Scrapes headlines and links from CNET's HTML content.
    """
    articles = []
    soup = BeautifulSoup(html, "html.parser")
    # Updated Selector: Trying <h3> as an alternative to <h2> for headlines.
    for h3 in soup.find_all("h3"):
        link = h3.find("a")
        if link and link.has_attr("href"):
            title = link.get_text(strip=True)
            url = link["href"]
            if title and url:
                # CNET links are usually relative
                if not url.startswith("http"):
                    url = "https://www.cnet.com" + url
                articles.append({"title": title, "url": url})
    print(f"Scraped {len(articles)} articles from CNET.")
    return articles

def scrape_thenextweb(html):
    """
    Scrapes headlines and links from The Next Web's HTML content.
    """
    articles = []
    soup = BeautifulSoup(html, "html.parser")
    # The Next Web also tends to use <h2> for its headlines.
    for h2 in soup.find_all("h2"):
        link = h2.find("a")
        if link and link.has_attr("href"):
            title = link.get_text(strip=True)
            url = link["href"]
            if title and url:
                if not url.startswith("http"):
                    url = "https://thenextweb.com" + url
                articles.append({"title": title, "url": url})
    print(f"Scraped {len(articles)} articles from The Next Web.")
    return articles

def send_summary_email(status_message):
    """
    Sends a summary email indicating the script ran but found no new articles.
    """
    if not all([SENDER_EMAIL, SENDER_APP_PASSWORD, RECIPIENT_EMAIL]) or \
       "your_email@gmail.com" in SENDER_EMAIL or "your_app_password" in SENDER_APP_PASSWORD:
        print("Error: Email credentials are not fully configured. Cannot send summary email.")
        return False

    subject = "Browser Agent Run Summary"
    body = f"The browser detection agent completed its run.\n\nStatus: {status_message}"

    msg = MIMEMultipart()
    msg["From"] = SENDER_EMAIL
    msg["To"] = RECIPIENT_EMAIL
    msg["Subject"] = subject
    msg.attach(MIMEText(body, "plain"))

    try:
        print("Sending summary email...")
        with smtplib.SMTP_SSL("smtp.gmail.com", 465) as server:
            server.login(SENDER_EMAIL, SENDER_APP_PASSWORD)
            server.send_message(msg)
        print("Summary email sent successfully!")
        return True
    except smtplib.SMTPAuthenticationError:
        print("Error: SMTP authentication failed. Please check your SENDER_EMAIL and SENDER_APP_PASSWORD.")
    except Exception as e:
        print(f"An unexpected error occurred while sending summary email: {e}")

    return False

def scrape_articles_from_sites():
    """
    Iterates through the WEBSITES list, fetches HTML, and scrapes articles.
    """
    all_articles = []
    for url in WEBSITES:
        html = fetch_html(url)
        if not html:
            continue

        if "techcrunch.com" in url:
            all_articles.extend(scrape_techcrunch(html))
        elif "theverge.com" in url:
            all_articles.extend(scrape_theverge(html))
        elif "arstechnica.com" in url:
            all_articles.extend(scrape_arstechnica(html))
        elif "wired.com" in url:
            all_articles.extend(scrape_wired(html))
        elif "engadget.com" in url:
            all_articles.extend(scrape_engadget(html))
        elif "gizmodo.com" in url:
            all_articles.extend(scrape_gizmodo(html))
        elif "cnet.com" in url:
            all_articles.extend(scrape_cnet(html))
        elif "thenextweb.com" in url:
            all_articles.extend(scrape_thenextweb(html))

    return all_articles


# --- Helper Functions ---

def filter_articles(articles):
    """
    Filters articles based on predefined positive and negative keywords.
    """
    positive_keywords = ["new browser", "launches browser", "unveils browser", "browser beta", "browser 1.0"]
    negative_keywords = ["update", "security", "extension", "chrome", "firefox", "edge", "safari"]

    filtered_articles = []
    print(f"Filtering {len(articles)} articles...")

    for article in articles:
        headline = article.get("title", "").lower()
        has_positive_match = any(keyword in headline for keyword in positive_keywords)
        if has_positive_match:
            has_negative_match = any(keyword in headline for keyword in negative_keywords)
            if not has_negative_match:
                print(f"  [+] Found potential match: {article['title']}")
                filtered_articles.append(article)

    print(f"Found {len(filtered_articles)} matching articles after filtering.")
    return filtered_articles

def load_sent_links():
    """
    Loads the list of already sent links from the tracking file.
    """
    try:
        with open(SENT_LINKS_FILE, "r") as f:
            return set(line.strip() for line in f)
    except FileNotFoundError:
        return set()

def save_sent_link(url):
    """
    Appends a new URL to the tracking file.
    """
    with open(SENT_LINKS_FILE, "a") as f:
        f.write(url + "\n")

def send_email_notification(article):
    """
    Sends an email notification for a new browser article.
    """
    if not all([SENDER_EMAIL, SENDER_APP_PASSWORD, RECIPIENT_EMAIL]) or \
       "your_email@gmail.com" in SENDER_EMAIL or "your_app_password" in SENDER_APP_PASSWORD:
        print("Error: Email credentials are not fully configured. Cannot send email.")
        return False

    subject = f"New Browser Alert: {article['title']}"
    body = f"A new potential browser was found.\n\nTitle: {article['title']}\nLink: {article['url']}"

    msg = MIMEMultipart()
    msg["From"] = SENDER_EMAIL
    msg["To"] = RECIPIENT_EMAIL
    msg["Subject"] = subject
    msg.attach(MIMEText(body, "plain"))

    try:
        print(f"Connecting to Gmail SMTP server to send email to {RECIPIENT_EMAIL}...")
        with smtplib.SMTP_SSL("smtp.gmail.com", 465) as server:
            server.login(SENDER_EMAIL, SENDER_APP_PASSWORD)
            server.send_message(msg)
        print("Email sent successfully!")
        return True
    except smtplib.SMTPAuthenticationError:
        print("Error: SMTP authentication failed. Please check your SENDER_EMAIL and SENDER_APP_PASSWORD.")
    except Exception as e:
        print(f"An unexpected error occurred while sending email: {e}")

    return False

# --- Main Script Logic ---

def main():
    """
    Main function to run the browser launch detection agent.
    """
    print("Starting the browser launch detection agent...")
    sent_links = load_sent_links()
    print(f"Loaded {len(sent_links)} previously sent links.")

    articles = scrape_articles_from_sites()

    if not articles:
        log_activity("Agent run finished. No articles were scraped.")
        print("No articles were scraped. Exiting.")
        return

    filtered_articles = filter_articles(articles)

    new_articles_to_notify = [
        article for article in filtered_articles if article["url"] not in sent_links
    ]

    notifications_sent = 0
    if new_articles_to_notify:
        print(f"Found {len(new_articles_to_notify)} new articles to process.")
        for article in new_articles_to_notify:
            email_sent = send_email_notification(article)
            if email_sent:
                notifications_sent += 1
                save_sent_link(article["url"])
                print(f"Link saved for: {article['title']}")

    # If no notifications were sent, send a summary email
    if notifications_sent == 0:
        summary_message = "Agent run finished. No new articles found."
        send_summary_email(summary_message)
        log_activity(summary_message)
        print(summary_message)
    else:
        summary_message = f"Agent run finished. Sent notifications for {notifications_sent} new article(s)."
        log_activity(summary_message)
        print(summary_message)

    print("Agent run complete.")

if __name__ == "__main__":
    main()