# Section B - Theory Questions

---

## Question 1

**"Document how scaling Azure PaaS/Serverless applications (Azure App Service, Azure Functions, Azure SQL DB) differs from scaling applications in a traditional IaaS model."**

When you scale an application on Azure PaaS or serverless, most of the heavy lifting is handled for you. You don't need to worry about provisioning servers, configuring load balancers, or deciding how many virtual machines to spin up. Azure does that automatically based on the demand your app is receiving.

Take Azure App Service for example. You can set an autoscale rule that says "if CPU usage goes above 70% for 5 minutes, add another instance." Azure handles the rest. You don't touch a server, you don't install anything, you just set the rule and move on. Azure Functions takes this even further because it scales to zero when there's no traffic, meaning you're not paying for anything when nobody is using the app. Azure SQL Database also scales on demand, you just pick a tier and Azure manages the underlying database engine.

With a traditional IaaS model it's a completely different story. IaaS means you rent virtual machines from a cloud provider and you are responsible for everything that runs on them. If your app starts getting more traffic, you need to manually spin up new VMs, install your application on them, configure a load balancer to distribute traffic between them, and then monitor everything yourself. If traffic drops you need to remember to shut those VMs down or you keep paying for them. Scaling in IaaS is a manual, time consuming process and it requires a lot more technical knowledge to get right.

The key difference is control vs convenience. PaaS and serverless take away the manual work and let you focus on building your application. IaaS gives you full control over the environment but expects you to manage everything yourself.

---

## Question 2

**"Discuss the concepts of platform-level vs infrastructure-level scaling, focusing on managed services vs self-managed infrastructure and explain the impact these choices have on deployment time, management overhead, cost."**

**Platform-level scaling** is what you get with managed services like Azure App Service, Azure Functions, and Azure SQL Database. The cloud provider manages the underlying infrastructure for you, including the servers, operating systems, networking, and security patches. You just deploy your code and configure how you want it to scale. This is sometimes called the "managed services" approach.

**Infrastructure-level scaling** is what you deal with when using IaaS like Azure Virtual Machines. You have full access to the operating system and can configure everything exactly how you want, but that also means you are responsible for patching, monitoring, load balancing, and scaling everything manually.

**Impact on deployment time:**
With managed services, deployment is fast. You can push your app to Azure App Service in a few minutes directly from Visual Studio. With IaaS, before you even deploy your app you need to set up and configure your virtual machines, which can take hours depending on complexity.

**Impact on management overhead:**
Managed services have very low management overhead. Azure handles OS updates, security patches, and server maintenance. With IaaS you need a team (or at least someone) dedicated to managing the infrastructure, applying updates, and making sure everything stays online.

**Impact on cost:**
This one is more nuanced. Managed services can feel more expensive at first glance because you're paying for convenience. Azure Functions however uses a consumption model where you only pay per execution, which can be very cheap for low-traffic apps. IaaS VMs run 24/7 so even if your app is idle you're paying the full VM cost. For applications with unpredictable or low traffic, serverless and PaaS are usually cheaper. For very large, stable, high-traffic applications, IaaS can sometimes be more cost effective because you have more control over what you're running.

Overall, platform-level scaling is the better choice for most modern applications because the time saved on managing infrastructure far outweighs the extra cost, especially for student projects and small to medium businesses.

---

## Question 3

**"Use a comparison table to contrast Azure PaaS/Serverless scaling with traditional IaaS scaling."**

| Aspect | Azure PaaS / Serverless | Traditional IaaS |
|---|---|---|
| **Who manages the servers** | Azure manages it for you | You manage it yourself |
| **Scaling method** | Automatic, rule-based or event-driven | Manual, you provision new VMs yourself |
| **Setup time** | Minutes | Hours to days |
| **OS and security patches** | Handled by Azure automatically | Your responsibility |
| **Cost model** | Pay for what you use (consumption based) | Pay for VMs 24/7 whether used or not |
| **Minimum cost when idle** | Near zero (Functions scale to zero) | Full VM cost still running |
| **Control over environment** | Limited, Azure controls the OS | Full control over OS and config |
| **Load balancing** | Built in and automatic | Must configure manually |
| **Deployment complexity** | Low, deploy code directly | High, configure infrastructure first |
| **Management overhead** | Very low | High, requires dedicated ops work |
| **Best suited for** | Most modern apps, startups, student projects | Legacy apps, very specific OS requirements |
| **Example Azure services** | App Service, Azure Functions, Azure SQL DB | Azure Virtual Machines, Azure VM Scale Sets |

As shown in the table above, the main tradeoff between PaaS/Serverless and IaaS is control vs convenience. PaaS and serverless remove the need to manage infrastructure which saves a lot of time and reduces complexity, making them the go-to choice for most modern cloud applications. IaaS is still useful when you need full control over your environment, but it comes with a significantly higher management burden and less flexibility when it comes to scaling quickly in response to traffic spikes.
