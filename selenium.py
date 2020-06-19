from selenium import webdriver


def get_web_driver():
    options = webdriver.ChromeOptions()
    options.add_argument("--start-maximized")
    return webdriver.Chrome('c:/Users/ddrozdov/Downloads/chromedriver.exe', options=options)


def login_to_vodafone(driver):
    driver.get('https://auth.myvodafone.com.au/login?code=uxr')
    driver.find_element_by_id('userid').send_keys('0418268273')
    driver.find_element_by_id('password').send_keys('Dmytro1128')
    driver.find_element_by_id('loginButton').click()


def main():
    driver = get_web_driver()
    login_to_vodafone(driver)
    m = 1


if __name__ == '__main__':
    main()